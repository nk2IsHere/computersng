using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class TierTests {
    private readonly GameFixture _fixture;

    public TierTests(GameFixture fixture) {
        _fixture = fixture;
    }

    private static string AddressOf(string id) {
        return id[(id.LastIndexOf('.') + 1)..];
    }

    private static bool GroupHasChestAt(JObject result, int x, int y) {
        return result["group"]!["members"]!.Any(member =>
            member["kind"]!.Value<string>() == "chest"
            && member["x"]!.Value<int>() == x
            && member["y"]!.Value<int>() == y);
    }

    // The same layout for both tiers, a furnace beside the controller and a chest
    // beside the furnace, two tiles from the controller. The basic tier only reaches
    // adjacent tiles, the advanced tier flood fills through the furnace to the chest.
    [E2eFact]
    public void BasicReachesAdjacentAndAdvancedFloodFills() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var y = ey + 10;

        var (basicX, basicControllerX) = (ex - 8, ex - 4);
        client.Place("computer", basicX, y);
        client.Place("router", ex - 6, y);
        var basicController = AddressOf(client.Place("machineController", basicControllerX, y));
        client.PlaceVanilla("furnace", basicControllerX + 1, y);
        client.PlaceChest(basicControllerX + 2, y, new[] { ("388", 1) });

        var (advancedX, advancedControllerX) = (ex + 2, ex + 6);
        client.Place("computer", advancedX, y);
        client.Place("router", ex + 4, y);
        var advancedController = AddressOf(client.Place("advancedMachineController", advancedControllerX, y));
        client.PlaceVanilla("furnace", advancedControllerX + 1, y);
        client.PlaceChest(advancedControllerX + 2, y, new[] { ("388", 1) });
        client.WaitTicks(60);

        client.WriteDisk(basicX, y, "/Startup.js", Programs.GroupLister);
        client.WriteDisk(basicX, y, "/controller.txt", basicController);
        client.WriteDisk(basicX, y, "/nonce.txt", "basic");
        client.RestartComputer(basicX, y);
        var basic = Waits.ResultForNonce(client, basicX, y, "/group.json", "basic", TimeSpan.FromSeconds(90));
        Assert.True(basic["ok"]!.Value<bool>(), basic.ToString());
        Assert.Contains(basic["group"]!["members"]!, member => member["kind"]!.Value<string>() == "machine");
        Assert.False(GroupHasChestAt(basic, basicControllerX + 2, y), basic.ToString());

        client.WriteDisk(advancedX, y, "/Startup.js", Programs.GroupLister);
        client.WriteDisk(advancedX, y, "/controller.txt", advancedController);
        client.WriteDisk(advancedX, y, "/nonce.txt", "advanced");
        client.RestartComputer(advancedX, y);
        var advanced = Waits.ResultForNonce(client, advancedX, y, "/group.json", "advanced", TimeSpan.FromSeconds(90));
        Assert.True(advanced["ok"]!.Value<bool>(), advanced.ToString());
        Assert.True(GroupHasChestAt(advanced, advancedControllerX + 2, y), advanced.ToString());
    }

    [E2eFact]
    public void SensorRadiusCapsFollowTheTier() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var y = ey + 11;
        var (x, routerX) = (ex - 8, ex - 6);
        // The reach test's clusters already provide computers and routers on the row
        // above, but this test keeps its own so the two stay independent.
        client.Place("computer", x, y);
        client.Place("router", routerX, y);
        var basicSensor = AddressOf(client.Place("playerSensor", ex - 4, y));
        var advancedSensor = AddressOf(client.Place("advancedPlayerSensor", ex - 2, y));

        client.WriteDisk(x, y, "/Startup.js", Programs.SensorCapProbe);

        client.WriteDisk(x, y, "/sensor.txt", basicSensor);
        client.WriteDisk(x, y, "/limits.txt", "{\"accepted\":8,\"rejected\":9}");
        client.WriteDisk(x, y, "/nonce.txt", "basic");
        client.RestartComputer(x, y);
        var basic = Waits.ResultForNonce(client, x, y, "/sensor.json", "basic", TimeSpan.FromSeconds(90));
        Assert.True(basic["accepted"]!["ok"]!.Value<bool>(), basic.ToString());
        Assert.False(basic["rejected"]!["ok"]!.Value<bool>(), basic.ToString());
        Assert.Contains("between 1 and 8", basic["rejected"]!["error"]!.Value<string>());
        Assert.Equal(8, basic["info"]!["data"]!["radius"]!.Value<int>());

        client.WriteDisk(x, y, "/sensor.txt", advancedSensor);
        client.WriteDisk(x, y, "/limits.txt", "{\"accepted\":64,\"rejected\":65}");
        client.WriteDisk(x, y, "/nonce.txt", "advanced");
        client.RestartComputer(x, y);
        var advanced = Waits.ResultForNonce(client, x, y, "/sensor.json", "advanced", TimeSpan.FromSeconds(90));
        Assert.True(advanced["accepted"]!["ok"]!.Value<bool>(), advanced.ToString());
        Assert.False(advanced["rejected"]!["ok"]!.Value<bool>(), advanced.ToString());
        Assert.Contains("between 1 and 64", advanced["rejected"]!["error"]!.Value<string>());
        Assert.Equal(64, advanced["info"]!["data"]!["radius"]!.Value<int>());
    }
}
