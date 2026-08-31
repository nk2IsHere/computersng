using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class ChestRearrangementTests {
    private readonly GameFixture _fixture;

    public ChestRearrangementTests(GameFixture fixture) {
        _fixture = fixture;
    }

    // A player picks a chest up and puts it down somewhere else next to the controller.
    // The group must follow the move. The program stamps its output with a nonce so the
    // test can tell the listing of each run apart.
    private const string Program =
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/group.json\")) Delete(\"/group.json\")\n" +
        "        WriteString(\"/group.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        try {\n" +
        "            const nonce = ReadString(\"/nonce.txt\")\n" +
        "            const controller = ReadString(\"/controller.txt\")\n" +
        "            const group = await Call(controller, \"list\")\n" +
        "            write({ ok: true, nonce, group })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    private JObject ListingForNonce(int x, int y, string nonce) {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(90);
        while (true) {
            try {
                var result = JObject.Parse(_fixture.Client.ReadDisk(x, y, "/group.json"));
                if (result["nonce"]?.Value<string>() == nonce) {
                    Assert.True(result["ok"]!.Value<bool>(), result.ToString());
                    return result;
                }
            } catch (E2eFailureException) {
            }
            if (DateTime.UtcNow >= deadline) {
                throw new TimeoutException($"no listing for nonce {nonce} appeared");
            }
            Thread.Sleep(1000);
        }
    }

    private static bool HasChestAt(JObject result, int x, int y) {
        return result["group"]!["members"]!.Any(member =>
            member["kind"]!.Value<string>() == "chest"
            && member["x"]!.Value<int>() == x
            && member["y"]!.Value<int>() == y);
    }

    [E2eFact]
    public void GroupFollowsAChestMovedToAnotherTile() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex + 4, ey + 6);
        var controllerX = ex + 8;
        client.Place("computer", x, y);
        client.Place("router", ex + 6, y);
        var controllerId = client.Place("machineController", controllerX, y);
        client.PlaceChest(controllerX, y + 1, new[] { ("388", 3) });
        client.WaitTicks(60);

        client.WriteDisk(x, y, "/controller.txt", controllerId[(controllerId.LastIndexOf('.') + 1)..]);
        client.WriteDisk(x, y, "/Startup.js", Program);
        client.WriteDisk(x, y, "/nonce.txt", "before");
        client.RestartComputer(x, y);
        var before = ListingForNonce(x, y, "before");
        Assert.True(HasChestAt(before, controllerX, y + 1), before.ToString());

        client.Remove(controllerX, y + 1);
        client.PlaceChest(controllerX - 1, y, new[] { ("388", 3) });
        client.WaitTicks(60);

        client.WriteDisk(x, y, "/nonce.txt", "after");
        client.RestartComputer(x, y);
        var after = ListingForNonce(x, y, "after");
        Assert.True(HasChestAt(after, controllerX - 1, y), after.ToString());
        Assert.False(HasChestAt(after, controllerX, y + 1), after.ToString());
    }
}
