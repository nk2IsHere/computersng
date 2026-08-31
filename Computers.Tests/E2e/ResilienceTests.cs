using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class ResilienceTests {
    private readonly GameFixture _fixture;

    public ResilienceTests(GameFixture fixture) {
        _fixture = fixture;
    }

    private const string CrashingProgram =
        "export async function Main() {\n" +
        "    throw new Error(\"boom\")\n" +
        "}\n";

    // Burns the statement budget of every slice without ever yielding, which trips the
    // watchdog and sends the computer into a reboot cycle.
    private const string RunawayProgram =
        "export async function Main() {\n" +
        "    for (;;) {\n" +
        "    }\n" +
        "}\n";

    private const string MarkerProgram =
        "import { WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    if (Exists(\"/result.json\")) Delete(\"/result.json\")\n" +
        "    WriteString(\"/result.json\", JSON.stringify({ ok: true, marker: \"recovered\" }))\n" +
        "}\n";

    [E2eFact]
    public void CrashingStartupLeavesTheComputerUsable() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 8, ey + 14);
        client.Place("computer", x, y);
        client.WriteDisk(x, y, "/Startup.js", CrashingProgram);
        client.RestartComputer(x, y);
        client.WaitTicks(120);

        // The OS catches startup errors, so the computer stays alive and a fixed
        // program installed afterwards runs normally.
        client.WriteDisk(x, y, "/Startup.js", MarkerProgram);
        client.RestartComputer(x, y);
        var result = JObject.Parse(client.PollDisk(x, y, "/result.json", TimeSpan.FromSeconds(60)));
        Assert.Equal("recovered", result["marker"]!.Value<string>());
    }

    [E2eFact]
    public void RunawayProgramTripsTheWatchdogAndRecoversFromADiskFix() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 6, ey + 14);
        client.Place("computer", x, y);
        client.WriteDisk(x, y, "/Startup.js", RunawayProgram);
        client.RestartComputer(x, y);

        // While the computer thrashes through watchdog reboots the host must stay
        // responsive.
        client.WaitTicks(300);
        Assert.True(client.Status()["worldReady"]!.Value<bool>());

        // Overwriting the startup program is enough, the next watchdog reboot picks it
        // up without any harness restart.
        client.WriteDisk(x, y, "/Startup.js", MarkerProgram);
        var result = JObject.Parse(client.PollDisk(x, y, "/result.json", TimeSpan.FromSeconds(120)));
        Assert.Equal("recovered", result["marker"]!.Value<string>());
    }
}
