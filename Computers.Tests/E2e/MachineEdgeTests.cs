using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class MachineEdgeTests {
    private readonly GameFixture _fixture;

    public MachineEdgeTests(GameFixture fixture) {
        _fixture = fixture;
    }

    private static string AddressOf(string id) {
        return id[(id.LastIndexOf('.') + 1)..];
    }

    [E2eFact]
    public void MachineOpsFailReadablyAndTheGroupFollowsRemoval() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var y = ey + 12;
        var (x, controllerX) = (ex - 8, ex - 4);
        var furnaceX = controllerX + 1;
        client.Place("computer", x, y);
        client.Place("router", ex - 6, y);
        var controller = AddressOf(client.Place("machineController", controllerX, y));
        client.PlaceVanilla("furnace", furnaceX, y);
        client.PlaceChest(controllerX, y + 1, new[] { ("378", 10), ("382", 5) });
        client.WaitTicks(60);

        client.WriteDisk(x, y, "/Startup.js", Programs.MachineOpEdges);
        client.WriteDisk(x, y, "/controller.txt", controller);
        client.WriteDisk(x, y, "/machine.txt", $"{{\"x\":{furnaceX},\"y\":{y}}}");
        client.WriteDisk(x, y, "/nonce.txt", "edges");
        client.RestartComputer(x, y);

        var ops = Waits.ResultForNonce(client, x, y, "/ops.json", "edges", TimeSpan.FromSeconds(120));
        Assert.True(ops["ok"]!.Value<bool>(), ops.ToString());

        // Inserting an item that is not in the group chests fails readably.
        Assert.False(ops["unknownItem"]!["ok"]!.Value<bool>(), ops.ToString());
        Assert.False(string.IsNullOrEmpty(ops["unknownItem"]!["error"]!.Value<string>()));

        // Collecting from an idle machine reports nothing to collect rather than crashing.
        var collectEmpty = ops["collectEmpty"]!;
        if (collectEmpty["ok"]!.Value<bool>()) {
            Assert.Empty(collectEmpty["data"]!["moved"]!.AsEnumerable());
        } else {
            Assert.False(string.IsNullOrEmpty(collectEmpty["error"]!.Value<string>()));
        }

        // A real insert works, and a second insert into the now working machine fails
        // readably because a machine holds one job.
        Assert.True(ops["insert"]!["ok"]!.Value<bool>(), ops.ToString());
        Assert.False(ops["insertBusy"]!["ok"]!.Value<bool>(), ops.ToString());
        Assert.False(string.IsNullOrEmpty(ops["insertBusy"]!["error"]!.Value<string>()));

        // Removing the machine drops it from the group on the next listing.
        client.Remove(furnaceX, y);
        client.WaitTicks(60);
        client.WriteDisk(x, y, "/Startup.js", Programs.GroupLister);
        client.WriteDisk(x, y, "/nonce.txt", "removed");
        client.RestartComputer(x, y);
        var listing = Waits.ResultForNonce(client, x, y, "/group.json", "removed", TimeSpan.FromSeconds(90));
        Assert.True(listing["ok"]!.Value<bool>(), listing.ToString());
        Assert.DoesNotContain(listing["group"]!["members"]!, member =>
            member["kind"]!.Value<string>() == "machine"
            && member["x"]!.Value<int>() == furnaceX
            && member["y"]!.Value<int>() == y);
    }
}
