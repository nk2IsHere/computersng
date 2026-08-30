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
        "import { Discover, GetRouters, ConfigureRouter } from \"./Core/Network\"\n" +
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/configured.json\")) Delete(\"/configured.json\")\n" +
        "        WriteString(\"/configured.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        try {\n" +
        "            let configured = false\n" +
        "            for (const router of GetRouters()) {\n" +
        "                const info = await Call(router.address, \"ping\")\n" +
        "                if (info.tier === \"advanced\") {\n" +
        "                    await ConfigureRouter(router.address, 3)\n" +
        "                    configured = true\n" +
        "                    break\n" +
        "                }\n" +
        "            }\n" +
        "            if (!configured) {\n" +
        "                write({ ok: false, error: \"no advanced router in range\" })\n" +
        "                return\n" +
        "            }\n" +
        "            const endpoints = await Discover()\n" +
        "            for (const address of endpoints) {\n" +
        "                const info = await Call(address, \"ping\")\n" +
        "                if (info.type === \"playerSensor\" && info.tier === \"advanced\") {\n" +
        "                    await Call(address, \"configure\", { radius: 5 })\n" +
        "                    write({ ok: true })\n" +
        "                    return\n" +
        "                }\n" +
        "            }\n" +
        "            write({ ok: false, error: \"no advanced player sensor found\" })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    private const string ReadbackProgram =
        "import { Discover, GetRouters } from \"./Core/Network\"\n" +
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/readback.json\")) Delete(\"/readback.json\")\n" +
        "        WriteString(\"/readback.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        try {\n" +
        "            let channel = null\n" +
        "            for (const router of GetRouters()) {\n" +
        "                const info = await Call(router.address, \"ping\")\n" +
        "                if (info.tier === \"advanced\") {\n" +
        "                    channel = info.channel\n" +
        "                    break\n" +
        "                }\n" +
        "            }\n" +
        "            const endpoints = await Discover()\n" +
        "            let radius = null\n" +
        "            for (const address of endpoints) {\n" +
        "                const info = await Call(address, \"ping\")\n" +
        "                if (info.type === \"playerSensor\" && info.tier === \"advanced\") {\n" +
        "                    radius = info.radius\n" +
        "                }\n" +
        "            }\n" +
        "            write({ ok: true, channel, radius })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    [E2eFact]
    public void StateSurvivesSaveAndColdReload() {
        var client = _fixture.Client;
        var computerId = client.Place("computer", 62, 20);
        var routerId = client.Place("advancedRouter", 64, 20);
        var sensorId = client.Place("advancedPlayerSensor", 66, 20);

        client.WriteDisk(62, 20, "/Startup.js", ConfigureProgram);
        client.RestartComputer(62, 20);
        var configured = JObject.Parse(client.PollDisk(62, 20, "/configured.json", TimeSpan.FromSeconds(90)));
        Assert.True(configured["ok"]!.Value<bool>(), configured.ToString());

        client.WriteDisk(62, 20, "/marker.txt", "survives");
        client.SaveGame();
        _fixture.RestartGame();
        client = _fixture.Client;

        Assert.Equal("survives", client.ReadDisk(62, 20, "/marker.txt"));
        Assert.Equal(computerId, client.QueryObject(62, 20)["id"]!.Value<string>());
        Assert.Equal(routerId, client.QueryObject(64, 20)["id"]!.Value<string>());
        Assert.Equal(sensorId, client.QueryObject(66, 20)["id"]!.Value<string>());

        client.WriteDisk(62, 20, "/Startup.js", ReadbackProgram);
        client.RestartComputer(62, 20);
        var readback = JObject.Parse(client.PollDisk(62, 20, "/readback.json", TimeSpan.FromSeconds(90)));
        Assert.True(readback["ok"]!.Value<bool>(), readback.ToString());
        Assert.Equal(3, readback["channel"]!.Value<int>());
        Assert.Equal(5, readback["radius"]!.Value<int>());
    }
}
