using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class ShippingEdgeTests {
    private readonly GameFixture _fixture;

    public ShippingEdgeTests(GameFixture fixture) {
        _fixture = fixture;
    }

    private static string AddressOf(string id) {
        return id[(id.LastIndexOf('.') + 1)..];
    }

    private const string Program =
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/shipping.json\")) Delete(\"/shipping.json\")\n" +
        "        WriteString(\"/shipping.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const attempt = async (fn) => {\n" +
        "        try {\n" +
        "            return { ok: true, data: await fn() }\n" +
        "        } catch (e) {\n" +
        "            return { ok: false, error: String(e) }\n" +
        "        }\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        const nonce = Exists(\"/nonce.txt\") ? ReadString(\"/nonce.txt\") : \"none\"\n" +
        "        try {\n" +
        "            const controller = ReadString(\"/controller.txt\")\n" +
        "            const price = await attempt(() => Call(controller, \"price\", { itemId: \"388\" }))\n" +
        "            const badPrice = await attempt(() => Call(controller, \"price\", { itemId: \"notanitem\" }))\n" +
        "            const sellAbsent = await attempt(() => Call(controller, \"sell\", { itemId: \"390\" }))\n" +
        "            const sellOverCount = await attempt(() => Call(controller, \"sell\", { itemId: \"388\", count: 10 }))\n" +
        "            write({ ok: true, nonce, price, badPrice, sellAbsent, sellOverCount })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, nonce, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    [E2eFact]
    public void PricingAndSellingFailReadablyAndClampToStock() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var y = ey + 16;
        var (x, controllerX) = (ex - 8, ex - 4);
        client.Place("computer", x, y);
        client.Place("router", ex - 6, y);
        var controller = AddressOf(client.Place("shippingController", controllerX, y));
        client.PlaceChest(controllerX, y + 1, new[] { ("388", 5) });
        client.WaitTicks(60);

        client.WriteDisk(x, y, "/Startup.js", Program);
        client.WriteDisk(x, y, "/controller.txt", controller);
        client.WriteDisk(x, y, "/nonce.txt", "edges");
        client.RestartComputer(x, y);

        var result = Waits.ResultForNonce(client, x, y, "/shipping.json", "edges", TimeSpan.FromSeconds(120));
        Assert.True(result["ok"]!.Value<bool>(), result.ToString());

        Assert.True(result["price"]!["ok"]!.Value<bool>(), result.ToString());
        Assert.True(result["price"]!["data"]!["price"]!.Value<int>() > 0);

        Assert.False(result["badPrice"]!["ok"]!.Value<bool>(), result.ToString());
        Assert.Contains("unknown item", result["badPrice"]!["error"]!.Value<string>());

        Assert.False(result["sellAbsent"]!["ok"]!.Value<bool>(), result.ToString());
        Assert.Contains("not found in group chests", result["sellAbsent"]!["error"]!.Value<string>());

        // Selling more than the chest holds clamps to what is actually there.
        Assert.True(result["sellOverCount"]!["ok"]!.Value<bool>(), result.ToString());
        Assert.Equal(5, result["sellOverCount"]!["data"]!["sold"]!.Value<int>());
    }
}
