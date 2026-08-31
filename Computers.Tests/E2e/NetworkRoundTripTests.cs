using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class NetworkRoundTripTests {
    private readonly GameFixture _fixture;

    public NetworkRoundTripTests(GameFixture fixture) {
        _fixture = fixture;
    }

    private const string Program =
        "import { Discover } from \"./Core/Network\"\n" +
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/result.json\")) Delete(\"/result.json\")\n" +
        "        WriteString(\"/result.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        try {\n" +
        "            const endpoints = await Discover()\n" +
        "            for (const address of endpoints) {\n" +
        "                const info = await Call(address, \"ping\")\n" +
        "                if (info.type === \"weatherStation\") {\n" +
        "                    const reading = await Call(address, \"read\")\n" +
        "                    write({ ok: true, weather: reading.weather })\n" +
        "                    return\n" +
        "                }\n" +
        "            }\n" +
        "            write({ ok: false, error: \"no weather station found\", endpoints })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    [E2eFact]
    public void ComputerReadsWeatherOverTheNetwork() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 6, ey + 2);
        client.Place("computer", x, y);
        client.Place("router", ex - 4, y);
        client.Place("weatherStation", ex - 2, y);
        client.WriteDisk(x, y, "/Startup.js", Program);
        client.RestartComputer(x, y);
        var result = JObject.Parse(client.PollDisk(x, y, "/result.json", TimeSpan.FromSeconds(90)));
        Assert.True(result["ok"]!.Value<bool>(), result.ToString());
        Assert.False(string.IsNullOrEmpty(result["weather"]!.Value<string>()));
    }
}
