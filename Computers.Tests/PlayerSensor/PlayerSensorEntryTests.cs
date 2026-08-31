using Computers.Computer;
using Computers.Core;
using Computers.Peripheral;
using Computers.PlayerSensor;
using Computers.PlayerSensor.Domain;
using Computers.PlayerSensor.Domain.Wire;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Tests.TestDoubles;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.PlayerSensor;

public class PlayerSensorEntryTests {
    private static readonly Id FactoryId = "factory.playerSensor".AsId();
    private static readonly Id S1 = "sensor.s1".AsId();
    private static readonly Id R1 = "router.r1".AsId();

    private class FakeSensorWorld : ISensorWorld {
        public SensorReading? Reading { get; set; }
        public int? LastRadius { get; private set; }

        public SensorReading? ReadingFor(Placement placement, int radius) {
            LastRadius = radius;
            return Reading;
        }
    }

    private static Configuration TestConfiguration() => new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>(),
            MessageTtl = 16
        }
    };

    private static (
        PlayerSensorStatefulDataContextEntry Sensor,
        FakeSensorWorld World,
        FakeRouterPort Router
    ) Make(PeripheralTier tier = PeripheralTier.Basic) {
        var configuration = TestConfiguration();
        var routerPort = new FakeRouterPort(R1);
        var routers = new ContextLookup<IRouterPort>(
            () => new HashSet<ContextEntry<IRouterPort>> { new(R1, routerPort) });
        var registry = new NetworkRegistry(new TestMonitor(), configuration, routers);
        var world = new FakeSensorWorld();

        registry.Register(new Placement(R1, NodeRole.Router, "Farm", 0, 0));
        registry.Register(new Placement(S1, NodeRole.Endpoint, "Farm", 5, 0));

        var sensor = new PlayerSensorStatefulDataContextEntry(
            FactoryId, S1, new TestMonitor(), configuration, registry, routers, world, tier);
        sensor.Fire(new StartPeripheralEvent());
        return (sensor, world, routerPort);
    }

    private static void Send(PlayerSensorStatefulDataContextEntry sensor, string payload) {
        sensor.ReceiveDatagram(new Datagram(Guid.NewGuid(), "c1", "s1", 16, payload));
    }

    private static JObject LastPayload(FakeRouterPort router) =>
        JObject.Parse(router.Delivered[^1].Datagram.Payload);

    private static SensorReading Reading(params SensedCharacter[] characters) =>
        new(
            characters.Where(c => c.Kind == "player").ToList(),
            characters.Where(c => c.Kind == "npc").ToList()
        );

    [Fact]
    public void ReadReturnsPlayersAndNpcsAndUsesConfiguredRadius() {
        var (sensor, world, router) = Make();
        world.Reading = Reading(
            new SensedCharacter("player", "nk2", 5, 1),
            new SensedCharacter("npc", "Abigail", 6, 2)
        );
        Send(sensor, "{\"cid\":\"r1\",\"cmd\":\"read\"}");
        sensor.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.True(reply["ok"]!.Value<bool>());
        Assert.Equal("nk2", reply["data"]!["players"]![0]!["name"]!.Value<string>());
        Assert.Equal("Abigail", reply["data"]!["npcs"]![0]!["name"]!.Value<string>());
        Assert.Equal(8, world.LastRadius); // spec default radius
    }

    [Fact]
    public void PresenceChangesPushEnteredAndLeft() {
        var (sensor, world, router) = Make();
        world.Reading = Reading(new SensedCharacter("npc", "Abigail", 6, 2));
        Send(sensor, "{\"cid\":\"s1\",\"cmd\":\"subscribe\",\"events\":[\"presence\"]}");
        sensor.Fire(new TickPeripheralEvent(1)); // ack and baseline
        router.Delivered.Clear();

        sensor.Fire(new TickPeripheralEvent(2)); // unchanged, no push
        Assert.Empty(router.Delivered);

        world.Reading = Reading(new SensedCharacter("player", "nk2", 5, 1));
        sensor.Fire(new TickPeripheralEvent(3));

        var push = LastPayload(router);
        Assert.Equal("presence", push["event"]!.Value<string>());
        Assert.Equal("nk2", push["entered"]![0]!["name"]!.Value<string>());
        Assert.Equal("Abigail", push["left"]![0]!["name"]!.Value<string>());
    }

    [Fact]
    public void PingRepliesWithTypeAndRadius() {
        var (sensor, world, router) = Make();
        world.Reading = Reading();
        Send(sensor, "{\"cid\":\"p1\",\"cmd\":\"ping\"}");
        sensor.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.Equal("playerSensor", reply["data"]!["type"]!.Value<string>());
        Assert.Equal("basic", reply["data"]!["tier"]!.Value<string>());
        Assert.Equal(8, reply["data"]!["radius"]!.Value<int>());
    }

    [Fact]
    public void ConfigureChangesTheRadiusAndPersists() {
        var (sensor, world, router) = Make();
        world.Reading = Reading();
        Send(sensor, "{\"cid\":\"c1\",\"cmd\":\"configure\",\"radius\":3}");
        sensor.Fire(new TickPeripheralEvent(1)); // ack, radius applies from the next reading
        Assert.True(LastPayload(router)["ok"]!.Value<bool>());

        sensor.Fire(new TickPeripheralEvent(2));
        Assert.Equal(3, world.LastRadius);
        Assert.Equal(3, sensor.Radius);

        var state = sensor.Store(Computers.Core.Context.Empty);
        var (fresh, _, _) = Make();
        fresh.Restore(Computers.Core.Context.Empty, state);
        Assert.Equal(3, fresh.Radius);
    }

    [Fact]
    public void ConfigureBeyondTheTierCapFails() {
        var (sensor, world, router) = Make(tier: PeripheralTier.Basic);
        world.Reading = Reading();
        Send(sensor, "{\"cid\":\"c3\",\"cmd\":\"configure\",\"radius\":9}");
        sensor.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Equal("radius must be between 1 and 8 for this tier", reply["error"]!.Value<string>());
    }

    [Fact]
    public void AdvancedTierAcceptsLargeRadii() {
        var (sensor, world, _) = Make(tier: PeripheralTier.Advanced);
        world.Reading = Reading();
        Send(sensor, "{\"cid\":\"c4\",\"cmd\":\"configure\",\"radius\":40}");
        sensor.Fire(new TickPeripheralEvent(1));
        sensor.Fire(new TickPeripheralEvent(2));

        Assert.Equal(40, world.LastRadius);
    }

    [Fact]
    public void RestoredRadiusClampsToTheTierCap() {
        var (advanced, world, _) = Make(tier: PeripheralTier.Advanced);
        world.Reading = Reading();
        Send(advanced, "{\"cid\":\"c5\",\"cmd\":\"configure\",\"radius\":40}");
        advanced.Fire(new TickPeripheralEvent(1));

        var state = advanced.Store(Computers.Core.Context.Empty);
        var (basic, _, _) = Make(tier: PeripheralTier.Basic);
        basic.Restore(Computers.Core.Context.Empty, state);

        Assert.Equal(8, basic.Radius);
    }

    [Fact]
    public void ConfigureRejectsAnInvalidRadius() {
        var (sensor, world, router) = Make();
        world.Reading = Reading();
        Send(sensor, "{\"cid\":\"c2\",\"cmd\":\"configure\",\"radius\":0}");
        sensor.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Contains("radius must be at least 1", reply["error"]!.Value<string>());
        Assert.Equal(8, sensor.Radius);
    }
}
