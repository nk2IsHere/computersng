using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class ComputerProgramTests {
    private readonly GameFixture _fixture;

    public ComputerProgramTests(GameFixture fixture) {
        _fixture = fixture;
    }

    private const string Program =
        "import { WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    if (Exists(\"/result.json\")) Delete(\"/result.json\")\n" +
        "    WriteString(\"/result.json\", JSON.stringify({ ok: true, marker: \"alive\" }))\n" +
        "}\n";

    [E2eFact]
    public void StartupProgramWritesAMarkerFile() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 8, ey + 2);
        client.Place("computer", x, y);
        client.WriteDisk(x, y, "/Startup.js", Program);
        client.RestartComputer(x, y);
        var content = client.PollDisk(x, y, "/result.json", TimeSpan.FromSeconds(60));
        var result = JObject.Parse(content);
        Assert.True(result["ok"]!.Value<bool>());
        Assert.Equal("alive", result["marker"]!.Value<string>());
    }
}
