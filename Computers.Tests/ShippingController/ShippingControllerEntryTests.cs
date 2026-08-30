using Computers.Computer;
using Computers.Core;
using Computers.MachineController.Domain;
using Computers.Peripheral;
using Computers.Peripheral.Domain;
using Computers.Router;
using Computers.Router.Domain;
using Computers.ShippingController;
using Computers.ShippingController.Domain;
using Computers.ShippingController.Domain.Wire;
using Computers.Tests.TestDoubles;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.ShippingController;

public class ShippingControllerEntryTests {
    private static readonly Id FactoryId = "factory.shipping".AsId();
    private static readonly Id B1 = "shipping.b1".AsId();
    private static readonly Id R1 = "router.r1".AsId();

    private class GridWorld : IGroupWorld {
        private readonly Dictionary<(int X, int Y), GroupCellKind> _cells;
        public GridWorld(Dictionary<(int, int), GroupCellKind> cells) { _cells = cells; }
        public GroupCellKind KindAt(int x, int y) => _cells.GetValueOrDefault((x, y), GroupCellKind.Empty);
    }

    private class FakeShippingLocation : IShippingLocation {
        public IGroupWorld GroupWorld { get; set; } = new GridWorld(new());

        public IShippingAccess Access(MachineGroup group) => new FakeAccess(group);
    }

    private class FakeAccess : IShippingAccess {
        private readonly MachineGroup _group;
        public FakeAccess(MachineGroup group) { _group = group; }

        public IReadOnlyList<ShippedChest> Chests() => _group.Chests
            .Select(position => new ShippedChest(position.X, position.Y, new List<ShippedChestItem>()))
            .ToList();

        public SellResult Sell(string itemId, int? count) {
            if (itemId != "378") {
                throw new PeripheralRequestException($"item '{itemId}' not found in group chests");
            }
            var sold = count ?? 10;
            return new SellResult(itemId, "Copper Ore", sold, sold * 5);
        }
    }

    private class FakeShippingWorld : IShippingWorld {
        public FakeShippingLocation? Location { get; set; } = new();
        public IShippingLocation? LocationOf(Placement placement) => Location;
        public int? Price(string itemId) => itemId == "378" ? 5 : null;
    }

    private static Configuration TestConfiguration() => new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>(),
            MessageTtl = 16
        }
    };

    private static (ShippingControllerStatefulDataContextEntry Controller, FakeShippingWorld World, FakeRouterPort Router) Make(
        PeripheralTier tier = PeripheralTier.Advanced
    ) {
        var configuration = TestConfiguration();
        var routerPort = new FakeRouterPort(R1);
        var routers = new ContextLookup<IRouterPort>(
            () => new HashSet<ContextEntry<IRouterPort>> { new(R1, routerPort) });
        var registry = new NetworkRegistry(new TestMonitor(), configuration, routers);
        var world = new FakeShippingWorld();

        registry.Register(new Placement(R1, NodeRole.Router, "Farm", 0, 0));
        registry.Register(new Placement(B1, NodeRole.Endpoint, "Farm", 5, 0));

        var controller = new ShippingControllerStatefulDataContextEntry(
            FactoryId, B1, new TestMonitor(), configuration, registry, routers, world, tier);
        controller.Fire(new StartPeripheralEvent());
        return (controller, world, routerPort);
    }

    private static void Send(ShippingControllerStatefulDataContextEntry controller, string payload) {
        controller.ReceiveDatagram(new Datagram(Guid.NewGuid(), "c1", "b1", 16, payload));
    }

    private static JObject LastPayload(FakeRouterPort router) =>
        JObject.Parse(router.Delivered[^1].Datagram.Payload);

    [Fact]
    public void SellRepliesWithTheResult() {
        var (controller, _, router) = Make();
        Send(controller, "{\"cid\":\"s1\",\"cmd\":\"sell\",\"itemId\":\"378\",\"count\":4}");
        controller.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.True(reply["ok"]!.Value<bool>());
        Assert.Equal(4, reply["data"]!["sold"]!.Value<int>());
        Assert.Equal(20, reply["data"]!["value"]!.Value<int>());
    }

    [Fact]
    public void PriceAnswersAndRejectsUnknownItems() {
        var (controller, _, router) = Make();
        Send(controller, "{\"cid\":\"p1\",\"cmd\":\"price\",\"itemId\":\"378\"}");
        Send(controller, "{\"cid\":\"p2\",\"cmd\":\"price\",\"itemId\":\"nope\"}");
        controller.Fire(new TickPeripheralEvent(1));

        var replies = router.Delivered.Select(d => JObject.Parse(d.Datagram.Payload)).ToList();
        Assert.Equal(5, replies[0]["data"]!["price"]!.Value<int>());
        Assert.False(replies[1]["ok"]!.Value<bool>());
    }

    [Fact]
    public void ListShowsGroupChests() {
        var (controller, world, router) = Make();
        world.Location!.GroupWorld = new GridWorld(new() {
            [(5, 1)] = GroupCellKind.Chest
        });
        Send(controller, "{\"cid\":\"l1\",\"cmd\":\"list\"}");
        controller.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.Equal(5, reply["data"]!["chests"]![0]!["x"]!.Value<int>());
        Assert.Equal(1, reply["data"]!["chests"]![0]!["y"]!.Value<int>());
    }

    [Fact]
    public void CommandsWithoutAWorldLocationFail() {
        var (controller, world, router) = Make();
        world.Location = null;
        Send(controller, "{\"cid\":\"e1\",\"cmd\":\"list\"}");
        controller.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Contains("no world position", reply["error"]!.Value<string>());
    }

    [Fact]
    public void BasicTierSeesOnlyAdjacentChests() {
        var (controller, world, router) = Make(tier: PeripheralTier.Basic);
        world.Location!.GroupWorld = new GridWorld(new() {
            [(6, 0)] = GroupCellKind.Chest,
            [(7, 0)] = GroupCellKind.Chest
        });
        Send(controller, "{\"cid\":\"t1\",\"cmd\":\"list\"}");
        controller.Fire(new TickPeripheralEvent(1));

        var chests = (JArray) LastPayload(router)["data"]!["chests"]!;
        Assert.Single(chests);
        Assert.Equal(6, chests[0]!["x"]!.Value<int>());
    }

    [Fact]
    public void AdvancedTierFloodFillsThroughMachines() {
        var (controller, world, router) = Make(tier: PeripheralTier.Advanced);
        world.Location!.GroupWorld = new GridWorld(new() {
            [(6, 0)] = GroupCellKind.Machine,
            [(7, 0)] = GroupCellKind.Chest
        });
        Send(controller, "{\"cid\":\"t2\",\"cmd\":\"list\"}");
        controller.Fire(new TickPeripheralEvent(1));

        var chests = (JArray) LastPayload(router)["data"]!["chests"]!;
        Assert.Single(chests);
        Assert.Equal(7, chests[0]!["x"]!.Value<int>());
    }

    [Fact]
    public void PingCarriesTheTier() {
        var (controller, _, router) = Make(tier: PeripheralTier.Basic);
        Send(controller, "{\"cid\":\"p3\",\"cmd\":\"ping\"}");
        controller.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.Equal("shippingController", reply["data"]!["type"]!.Value<string>());
        Assert.Equal("basic", reply["data"]!["tier"]!.Value<string>());
    }
}
