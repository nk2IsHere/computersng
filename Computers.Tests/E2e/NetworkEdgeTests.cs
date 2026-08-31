using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class NetworkEdgeTests {
    private readonly GameFixture _fixture;

    public NetworkEdgeTests(GameFixture fixture) {
        _fixture = fixture;
    }

    private static string AddressOf(string id) {
        return id[(id.LastIndexOf('.') + 1)..];
    }

    private void RunProbe(GameClient client, int x, int y, string nonce, string? location = null) {
        client.WriteDisk(x, y, "/nonce.txt", nonce, location);
        client.RestartComputer(x, y, location);
    }

    private static bool Reaches(JObject probe, string address, string type) {
        return probe["types"] is JObject types && types[address]?.Value<string>() == type;
    }

    [E2eFact]
    public void BasicRouterRefusesChannels() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 8, ey + 8);
        client.Place("computer", x, y);
        var basicRouterAddress = AddressOf(client.Place("router", ex - 6, y));
        client.WriteDisk(x, y, "/Startup.js", Programs.RouterConfigurer);
        client.WriteDisk(x, y, "/router.txt", basicRouterAddress);
        client.WriteDisk(x, y, "/channel.txt", "3");
        RunProbe(client, x, y, "basic");

        var result = Waits.ResultForNonce(client, x, y, "/configured.json", "basic", TimeSpan.FromSeconds(60));
        Assert.False(result["ok"]!.Value<bool>(), result.ToString());
        Assert.Contains("advanced router", result["error"]!.Value<string>());
    }

    [E2eFact]
    public void ChannelsBridgeLocationsAndPartitionAgain() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 4, ey + 8);
        client.Place("computer", x, y);
        var farmRouterAddress = AddressOf(client.Place("advancedRouter", ex - 2, y));
        client.WriteDisk(x, y, "/router.txt", farmRouterAddress);

        // The greenhouse cluster configures its own router from its own computer, the
        // way a player would walk over and do it.
        client.Place("computer", 5, 9, location: "Greenhouse");
        client.Place("advancedRouter", 7, 9, location: "Greenhouse");
        var stationAddress = AddressOf(client.Place("weatherStation", 9, 9, location: "Greenhouse"));
        client.WriteDisk(5, 9, "/Startup.js", Programs.RouterConfigurer, location: "Greenhouse");
        client.WriteDisk(5, 9, "/channel.txt", "5", location: "Greenhouse");
        client.WriteDisk(5, 9, "/nonce.txt", "greenhouse", location: "Greenhouse");
        client.RestartComputer(5, 9, location: "Greenhouse");
        var greenhouse = Waits.ResultForNonce(client, 5, 9, "/configured.json", "greenhouse", TimeSpan.FromSeconds(60), "Greenhouse");
        Assert.True(greenhouse["ok"]!.Value<bool>(), greenhouse.ToString());

        // Before the farm router shares the channel there is no bridge to the station.
        client.WriteDisk(x, y, "/Startup.js", Programs.NetworkProbe);
        RunProbe(client, x, y, "unbridged");
        var unbridged = Waits.ResultForNonce(client, x, y, "/probe.json", "unbridged", TimeSpan.FromSeconds(90));
        Assert.True(unbridged["ok"]!.Value<bool>(), unbridged.ToString());
        Assert.False(Reaches(unbridged, stationAddress, "weatherStation"), unbridged.ToString());

        // The same channel bridges the locations.
        client.WriteDisk(x, y, "/Startup.js", Programs.RouterConfigurer);
        client.WriteDisk(x, y, "/channel.txt", "5");
        RunProbe(client, x, y, "join");
        Waits.ResultForNonce(client, x, y, "/configured.json", "join", TimeSpan.FromSeconds(60));

        client.WriteDisk(x, y, "/Startup.js", Programs.NetworkProbe);
        RunProbe(client, x, y, "bridged");
        var bridged = Waits.ResultForNonce(client, x, y, "/probe.json", "bridged", TimeSpan.FromSeconds(90));
        Assert.True(Reaches(bridged, stationAddress, "weatherStation"), bridged.ToString());

        // Moving the farm router to another channel partitions the network again.
        client.WriteDisk(x, y, "/Startup.js", Programs.RouterConfigurer);
        client.WriteDisk(x, y, "/channel.txt", "9");
        RunProbe(client, x, y, "leave");
        Waits.ResultForNonce(client, x, y, "/configured.json", "leave", TimeSpan.FromSeconds(60));

        client.WriteDisk(x, y, "/Startup.js", Programs.NetworkProbe);
        RunProbe(client, x, y, "partitioned");
        var partitioned = Waits.ResultForNonce(client, x, y, "/probe.json", "partitioned", TimeSpan.FromSeconds(90));
        Assert.False(Reaches(partitioned, stationAddress, "weatherStation"), partitioned.ToString());
    }

    [E2eFact]
    public void NetworkSurvivesRouterRemovalAndReplacement() {
        // A private radio bubble far from every other cluster, so removing the router
        // really means going offline.
        var client = _fixture.Client;
        var (_, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (12, ey + 20);
        var routerX = 14;
        client.Place("computer", x, y);
        client.Place("router", routerX, y);
        var stationAddress = AddressOf(client.Place("weatherStation", 16, y));
        client.WriteDisk(x, y, "/Startup.js", Programs.NetworkProbe);

        RunProbe(client, x, y, "online");
        var online = Waits.ResultForNonce(client, x, y, "/probe.json", "online", TimeSpan.FromSeconds(90));
        Assert.True(Reaches(online, stationAddress, "weatherStation"), online.ToString());

        client.Remove(routerX, y);
        client.WaitTicks(30);
        RunProbe(client, x, y, "offline");
        var offline = Waits.ResultForNonce(client, x, y, "/probe.json", "offline", TimeSpan.FromSeconds(90));
        Assert.Empty(offline["routers"]!.AsEnumerable());
        Assert.False(Reaches(offline, stationAddress, "weatherStation"), offline.ToString());
        Assert.False(string.IsNullOrEmpty(offline["error"]?.Value<string>()) && offline["ok"]!.Value<bool>() == false, offline.ToString());

        client.Place("router", routerX, y);
        client.WaitTicks(30);
        RunProbe(client, x, y, "recovered");
        var recovered = Waits.ResultForNonce(client, x, y, "/probe.json", "recovered", TimeSpan.FromSeconds(90));
        Assert.True(Reaches(recovered, stationAddress, "weatherStation"), recovered.ToString());
    }

    [E2eFact]
    public void RadioDoesNotReachAcrossTheFarm() {
        // This cluster sits over twenty tiles from the router removal bubble, whose
        // station is the far target that must stay unreachable.
        var client = _fixture.Client;
        var (_, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (34, ey + 20);
        client.Place("computer", x, y);
        var ownRouterAddress = AddressOf(client.Place("router", 36, y));
        var farStationAddress = AddressOf(client.Place("weatherStation", 16, ey + 22));

        client.WriteDisk(x, y, "/Startup.js", Programs.NetworkProbe);
        RunProbe(client, x, y, "range");
        var probe = Waits.ResultForNonce(client, x, y, "/probe.json", "range", TimeSpan.FromSeconds(90));
        Assert.True(probe["ok"]!.Value<bool>(), probe.ToString());
        Assert.Contains(probe["routers"]!.AsEnumerable(), router => router["address"]!.Value<string>() == ownRouterAddress);
        Assert.False(Reaches(probe, farStationAddress, "weatherStation"), probe.ToString());
    }
}
