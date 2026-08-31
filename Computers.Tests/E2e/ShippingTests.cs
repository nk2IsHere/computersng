using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class ShippingTests {
    private readonly GameFixture _fixture;

    public ShippingTests(GameFixture fixture) {
        _fixture = fixture;
    }

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
        "            const sold = await Call(controller, \"sell\", { itemId: \"388\" })\n" +
        "            write({ ok: true, sold })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    [E2eFact]
    public void SoldItemLandsInTheShippingBin() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 8, ey + 4);
        client.Place("computer", x, y);
        client.Place("router", ex - 6, y);
        var controllerId = client.Place("shippingController", ex - 4, y);
        client.PlaceChest(ex - 4, y + 1, new[] { ("388", 5) });
        client.WaitTicks(60);
        client.WriteDisk(x, y, "/controller.txt", controllerId[(controllerId.LastIndexOf('.') + 1)..]);
        client.WriteDisk(x, y, "/Startup.js", Program);
        client.RestartComputer(x, y);
        var result = JObject.Parse(client.PollDisk(x, y, "/result.json", TimeSpan.FromSeconds(90)));
        Assert.True(result["ok"]!.Value<bool>(), result.ToString());
        Assert.Equal(5, result["sold"]!["sold"]!.Value<int>());

        // Other scenarios may ship wood on the same in game day, so the bin holds at
        // least this sale rather than exactly it.
        var bin = client.QueryShippingBin();
        var woodTotal = bin["items"]!
            .Where(item => item["itemId"]!.Value<string>() == "388")
            .Sum(item => item["count"]!.Value<int>());
        Assert.True(woodTotal >= 5, bin.ToString());
    }
}
