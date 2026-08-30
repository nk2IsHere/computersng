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
        client.Place("computer", 56, 18);
        client.WriteDisk(56, 18, "/Startup.js", Program);
        client.RestartComputer(56, 18);
        var content = client.PollDisk(56, 18, "/result.json", TimeSpan.FromSeconds(60));
        var result = JObject.Parse(content);
        Assert.True(result["ok"]!.Value<bool>());
        Assert.Equal("alive", result["marker"]!.Value<string>());
    }
}
