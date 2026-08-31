using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class MachineAutomationTests {
    private readonly GameFixture _fixture;

    public MachineAutomationTests(GameFixture fixture) {
        _fixture = fixture;
    }

    // The everyday automation loop a player would script. Feed a furnace copper ore
    // from a chest through the machine controller, wait for the smelt, collect the bar
    // back into the group chests.
    private const string Program =
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/result.json\")) Delete(\"/result.json\")\n" +
        "        WriteString(\"/result.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        try {\n" +
        "            const controller = ReadString(\"/controller.txt\")\n" +
        "            const group = await Call(controller, \"list\")\n" +
        "            const furnace = group.members.find(m => m.kind === \"machine\" && m.itemId === \"13\")\n" +
        "            const chest = group.members.find(m => m.kind === \"chest\" && m.items.some(i => i.id === \"378\"))\n" +
        "            if (!furnace || !chest) {\n" +
        "                write({ ok: false, error: \"furnace or ore chest missing from group\", group })\n" +
        "                return\n" +
        "            }\n" +
        "            const machine = { x: furnace.x, y: furnace.y }\n" +
        "            await Call(controller, \"insert\", { machine, itemId: \"378\", count: 1, fromChest: { x: chest.x, y: chest.y } })\n" +
        "            for (let attempt = 0; attempt < 60; attempt++) {\n" +
        "                const now = await Call(controller, \"list\")\n" +
        "                const state = now.members.find(m => m.kind === \"machine\" && m.x === machine.x && m.y === machine.y)\n" +
        "                if (state && state.state === \"ready\") {\n" +
        "                    const collected = await Call(controller, \"collect\", { machine })\n" +
        "                    const after = await Call(controller, \"list\")\n" +
        "                    write({ ok: true, collected, after })\n" +
        "                    return\n" +
        "                }\n" +
        "                for (let i = 0; i < 60; i++) await System.NextFrame()\n" +
        "            }\n" +
        "            write({ ok: false, error: \"furnace never became ready\" })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    [E2eFact]
    public void FurnaceSmeltsOreFedFromAChest() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 8, ey + 6);
        client.Place("computer", x, y);
        client.Place("router", ex - 6, y);
        var controllerId = client.Place("machineController", ex - 4, y);
        client.PlaceVanilla("furnace", ex - 3, y);
        client.PlaceChest(ex - 4, y + 1, new[] { ("378", 5), ("382", 2) });
        client.WaitTicks(60);

        client.WriteDisk(x, y, "/controller.txt", controllerId[(controllerId.LastIndexOf('.') + 1)..]);
        client.WriteDisk(x, y, "/Startup.js", Program);
        client.RestartComputer(x, y);
        var result = JObject.Parse(client.PollDisk(x, y, "/result.json", TimeSpan.FromSeconds(120)));
        Assert.True(result["ok"]!.Value<bool>(), result.ToString());

        var moved = result["collected"]!["moved"]!;
        Assert.Contains(moved, item => item["id"]!.Value<string>() == "334");

        var chest = client.QueryObject(ex - 4, y + 1)["chestItems"]!;
        Assert.Contains(chest, item => item["itemId"]!.Value<string>() == "334");
        Assert.DoesNotContain(chest, item => item["itemId"]!.Value<string>() == "378");
    }
}
