using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class SaveReloadTests {
    private readonly GameFixture _fixture;

    public SaveReloadTests(GameFixture fixture) {
        _fixture = fixture;
    }

    private const string ConfigureProgram =
        "import { ConfigureRouter } from \"./Core/Network\"\n" +
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/configured.json\")) Delete(\"/configured.json\")\n" +
        "        WriteString(\"/configured.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        try {\n" +
        "            const router = ReadString(\"/router.txt\")\n" +
        "            const sensor = ReadString(\"/sensor.txt\")\n" +
        "            await ConfigureRouter(router, 3)\n" +
        "            await Call(sensor, \"configure\", { radius: 5 })\n" +
        "            write({ ok: true, stamp: Date.now() })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    private const string ReadbackProgram =
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/readback.json\")) Delete(\"/readback.json\")\n" +
        "        WriteString(\"/readback.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        try {\n" +
        "            const router = ReadString(\"/router.txt\")\n" +
        "            const sensor = ReadString(\"/sensor.txt\")\n" +
        "            const routerInfo = await Call(router, \"ping\")\n" +
        "            const sensorInfo = await Call(sensor, \"ping\")\n" +
        "            write({ ok: true, channel: routerInfo.channel, radius: sensorInfo.radius })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    [E2eFact]
    public void StateSurvivesSaveAndColdReload() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 2, ey + 4);
        var computerId = client.Place("computer", x, y);
        var routerId = client.Place("advancedRouter", ex, y);
        var sensorId = client.Place("advancedPlayerSensor", ex + 2, y);

        client.WriteDisk(x, y, "/router.txt", routerId[(routerId.LastIndexOf('.') + 1)..]);
        client.WriteDisk(x, y, "/sensor.txt", sensorId[(sensorId.LastIndexOf('.') + 1)..]);
        client.WriteDisk(x, y, "/Startup.js", ConfigureProgram);
        client.RestartComputer(x, y);
        var configured = JObject.Parse(client.PollDisk(x, y, "/configured.json", TimeSpan.FromSeconds(90)));
        Assert.True(configured["ok"]!.Value<bool>(), configured.ToString());

        var stampBeforeReload = configured["stamp"]!.Value<long>();

        client.WriteDisk(x, y, "/marker.txt", "survives");
        client.SaveGame();
        _fixture.RestartGame();
        client = _fixture.Client;

        Assert.Equal("survives", client.ReadDisk(x, y, "/marker.txt"));

        // The persisted startup program must run again on its own after the reload,
        // which pins the storage restore path, a reloaded computer once served its
        // scripts an orphaned empty file system instead of the persisted one.
        Waits.Until(() => {
            try {
                var rebooted = JObject.Parse(client.ReadDisk(x, y, "/configured.json"));
                return rebooted["stamp"]!.Value<long>() != stampBeforeReload;
            } catch (E2eFailureException) {
                return false;
            }
        }, TimeSpan.FromSeconds(90), "the persisted startup program never reran after the reload");
        Assert.Equal(computerId, client.QueryObject(x, y)["id"]!.Value<string>());
        Assert.Equal(routerId, client.QueryObject(ex, y)["id"]!.Value<string>());
        Assert.Equal(sensorId, client.QueryObject(ex + 2, y)["id"]!.Value<string>());

        client.WriteDisk(x, y, "/Startup.js", ReadbackProgram);
        client.RestartComputer(x, y);
        var readback = JObject.Parse(client.PollDisk(x, y, "/readback.json", TimeSpan.FromSeconds(90)));
        Assert.True(readback["ok"]!.Value<bool>(), readback.ToString());
        Assert.Equal(3, readback["channel"]!.Value<int>());
        Assert.Equal(5, readback["radius"]!.Value<int>());
    }
}
