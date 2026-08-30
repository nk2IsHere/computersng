using Computers.Computer;
using Computers.Core;
using Computers.Peripheral;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Tests.TestDoubles;
using Computers.WeatherStation;
using Computers.WeatherStation.Domain;
using Computers.WeatherStation.Domain.Wire;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.WeatherStation;

public class WeatherStationEntryTests {
    private static readonly Id FactoryId = "factory.weatherStation".AsId();
    private static readonly Id W1 = "weather.w1".AsId();
    private static readonly Id R1 = "router.r1".AsId();

    private class FakeWeatherWorld : IWeatherWorld {
        public WeatherReading? Reading { get; set; }
        public WeatherReading? ReadingFor(Placement placement) => Reading;
    }

    private static WeatherReading Reading(int time = 600, int day = 1, string season = "spring") =>
        new(time, day, season, 1, "Sun", "Rain", 0.05);

    private static Configuration TestConfiguration() => new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>(),
            MessageTtl = 16
        }
    };

    private static (
        WeatherStationStatefulDataContextEntry Station,
        FakeWeatherWorld World,
        FakeRouterPort Router
    ) Make() {
        var configuration = TestConfiguration();
        var routerPort = new FakeRouterPort(R1);
        var routers = new ContextLookup<IRouterPort>(
            () => new HashSet<ContextEntry<IRouterPort>> { new(R1, routerPort) });
        var registry = new NetworkRegistry(new TestMonitor(), configuration, routers);
        var world = new FakeWeatherWorld();

        registry.Register(new Placement(R1, NodeRole.Router, "Farm", 0, 0));
        registry.Register(new Placement(W1, NodeRole.Endpoint, "Farm", 5, 0));

        var station = new WeatherStationStatefulDataContextEntry(
            FactoryId, W1, new TestMonitor(), configuration, registry, routers, world);
        station.Fire(new StartPeripheralEvent());
        return (station, world, routerPort);
    }

    private static void Send(WeatherStationStatefulDataContextEntry station, string payload) {
        station.ReceiveDatagram(new Datagram(Guid.NewGuid(), "c1", "w1", 16, payload));
    }

    private static JObject LastPayload(FakeRouterPort router) =>
        JObject.Parse(router.Delivered[^1].Datagram.Payload);

    [Fact]
    public void PingRepliesWithType() {
        var (station, world, router) = Make();
        world.Reading = Reading();
        Send(station, "{\"cid\":\"p1\",\"cmd\":\"ping\"}");
        station.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.True(reply["ok"]!.Value<bool>());
        Assert.Equal("weatherStation", reply["data"]!["type"]!.Value<string>());
    }

    [Fact]
    public void ReadReturnsTheReading() {
        var (station, world, router) = Make();
        world.Reading = Reading(time: 1300, day: 14, season: "summer");
        Send(station, "{\"cid\":\"r1\",\"cmd\":\"read\"}");
        station.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.True(reply["ok"]!.Value<bool>());
        Assert.Equal(1300, reply["data"]!["time"]!.Value<int>());
        Assert.Equal("summer", reply["data"]!["season"]!.Value<string>());
        Assert.Equal("Rain", reply["data"]!["weatherTomorrow"]!.Value<string>());
    }

    [Fact]
    public void ReadWithoutLocationFails() {
        var (station, world, router) = Make();
        world.Reading = null;
        Send(station, "{\"cid\":\"r2\",\"cmd\":\"read\"}");
        station.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Contains("no world position", reply["error"]!.Value<string>());
    }

    [Fact]
    public void DayChangePushesToDaySubscribers() {
        var (station, world, router) = Make();
        world.Reading = Reading(day: 1);
        Send(station, "{\"cid\":\"s1\",\"cmd\":\"subscribe\",\"events\":[\"day\"]}");
        station.Fire(new TickPeripheralEvent(1)); // ack and baseline
        router.Delivered.Clear();

        station.Fire(new TickPeripheralEvent(2)); // same day, no push
        Assert.Empty(router.Delivered);

        world.Reading = Reading(day: 2);
        station.Fire(new TickPeripheralEvent(3));

        var push = LastPayload(router);
        Assert.Equal("day", push["event"]!.Value<string>());
        Assert.Equal(2, push["reading"]!["day"]!.Value<int>());
    }

    [Fact]
    public void TimeChangePushesOnlyToTimeSubscribers() {
        var (station, world, router) = Make();
        world.Reading = Reading(time: 600);
        Send(station, "{\"cid\":\"s2\",\"cmd\":\"subscribe\",\"events\":[\"day\"]}");
        station.Fire(new TickPeripheralEvent(1)); // ack and baseline
        router.Delivered.Clear();

        world.Reading = Reading(time: 610);
        station.Fire(new TickPeripheralEvent(2)); // time changed, only day subscribed
        Assert.Empty(router.Delivered);

        Send(station, "{\"cid\":\"s3\",\"cmd\":\"subscribe\",\"events\":[\"time\"]}");
        station.Fire(new TickPeripheralEvent(3));
        router.Delivered.Clear();

        world.Reading = Reading(time: 620);
        station.Fire(new TickPeripheralEvent(4));

        var push = LastPayload(router);
        Assert.Equal("time", push["event"]!.Value<string>());
        Assert.Equal(620, push["reading"]!["time"]!.Value<int>());
    }
}
