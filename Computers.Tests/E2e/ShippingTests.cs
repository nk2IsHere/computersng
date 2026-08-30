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
        "                if (info.type === \"shippingController\") {\n" +
        "                    const sold = await Call(address, \"sell\", { itemId: \"388\" })\n" +
        "                    write({ ok: true, sold })\n" +
        "                    return\n" +
        "                }\n" +
        "            }\n" +
        "            write({ ok: false, error: \"no shipping controller found\" })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    [E2eFact]
    public void SoldItemLandsInTheShippingBin() {
        var client = _fixture.Client;
        client.Place("computer", 56, 20);
        client.Place("router", 58, 20);
        client.Place("shippingController", 60, 20);
        client.PlaceChest(60, 21, new[] { ("388", 5) });
        client.WaitTicks(60);
        client.WriteDisk(56, 20, "/Startup.js", Program);
        client.RestartComputer(56, 20);
        var result = JObject.Parse(client.PollDisk(56, 20, "/result.json", TimeSpan.FromSeconds(90)));
        Assert.True(result["ok"]!.Value<bool>(), result.ToString());
        var bin = client.QueryShippingBin();
        var wood = bin["items"]!.Single(item => item["itemId"]!.Value<string>() == "388");
        Assert.Equal(5, wood["count"]!.Value<int>());
    }
}
