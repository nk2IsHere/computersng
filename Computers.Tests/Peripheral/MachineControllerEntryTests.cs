using Computers.Computer;
using Computers.Core;
using Computers.Peripheral;
using Computers.MachineController;
using Computers.MachineController.Domain;
using Computers.MachineController.Domain.Wire;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Tests.TestDoubles;
using Newtonsoft.Json.Linq;
using Xunit;
using Context = Computers.Core.Context;

namespace Computers.Tests.Peripheral;

public class MachineControllerEntryTests {
    private static readonly Id FactoryId = "factory.peripheral".AsId();
    private static readonly Id P1 = "peripheral.p1".AsId();
    private static readonly Id R1 = "router.r1".AsId();

    // Echoes the scanned group back through list, so tier reach is observable.
    private class GroupEchoOps : IMachineGroupOps {
        private readonly MachineGroup _group;
        public GroupEchoOps(MachineGroup group) { _group = group; }
        public GroupSnapshot ListSnapshot() => new(
            _group.Truncated,
            _group.Machines
                .Select(m => (GroupMemberSnapshot) new MachineMemberSnapshot(m.X, m.Y, "id", "Machine", MachineState.Empty, null, 0))
                .ToList());
        public CollectResult Collect(MachinePosition? target) => new(new List<CollectedItem>());
        public InsertResult Insert(InsertRequest request) => new(request.Machine, "item");
        public void Rescan() { }
    }

    private class FakeGroupWorld : IGroupWorld {
        private readonly HashSet<(int X, int Y)> _machines;
        public FakeGroupWorld(HashSet<(int X, int Y)> machines) { _machines = machines; }
        public GroupCellKind KindAt(int x, int y) =>
            _machines.Contains((x, y)) ? GroupCellKind.Machine : GroupCellKind.Empty;
    }

    private class FakeMachineLocation : IMachineLocation {
        public HashSet<(int X, int Y)> Machines { get; } = new();
        public HashSet<(int X, int Y)> Ready { get; } = new();

        public IGroupWorld GroupWorld => new FakeGroupWorld(Machines);
        public IMachineGroupOps Ops(MachineGroup group, Action invalidateGroup) => new GroupEchoOps(group);
        public IReadOnlySet<(int X, int Y)> ReadyMachines(IEnumerable<(int X, int Y)> machines) =>
            machines.Where(Ready.Contains).ToHashSet();
        public MachineMemberSnapshot? Snapshot(int x, int y) =>
            new(x, y, "itemId", "Furnace", MachineState.Ready, null, 0);
    }

    private class FakeMachineWorld : IMachineWorld {
        public FakeMachineLocation? Location { get; set; }
        public IMachineLocation? LocationOf(Placement placement) => Location;
    }

    private static Configuration TestConfiguration() => new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>(),
            MessageTtl = 16
        }
    };

    private static (
        MachineControllerStatefulDataContextEntry Controller,
        FakeMachineWorld World,
        FakeRouterPort Router
    ) Make(bool withPlacement = false, PeripheralTier tier = PeripheralTier.Advanced) {
        var configuration = TestConfiguration();
        var routerPort = new FakeRouterPort(R1);
        var routers = new ContextLookup<IRouterPort>(
            () => new HashSet<ContextEntry<IRouterPort>> { new(R1, routerPort) });
        var registry = new NetworkRegistry(new TestMonitor(), configuration, routers);
        var world = new FakeMachineWorld();

        if (withPlacement) {
            registry.Register(new Placement(R1, NodeRole.Router, "Farm", 0, 0));
            registry.Register(new Placement(P1, NodeRole.Endpoint, "Farm", 5, 0));
        }

        var controller = new MachineControllerStatefulDataContextEntry(
            FactoryId, P1, new TestMonitor(), configuration, registry, routers, world, tier);
        return (controller, world, routerPort);
    }

    private static Datagram Incoming(string payload = "{}") =>
        new(Guid.NewGuid(), "c1", "p1", 16, payload);

    [Fact]
    public void ReceiveDatagramQueuesWhenEnabled() {
        var (controller, _, _) = Make();
        controller.Start();
        controller.ReceiveDatagram(Incoming());
        Assert.Equal(1, controller.PendingCommands);
    }

    [Fact]
    public void DisabledControllerDropsDatagrams() {
        var (controller, _, _) = Make();
        controller.ReceiveDatagram(Incoming()); // never started
        Assert.Equal(0, controller.PendingCommands);
    }

    [Fact]
    public void StopClearsTheInbox() {
        var (controller, _, _) = Make();
        controller.Start();
        controller.ReceiveDatagram(Incoming());
        controller.Stop();
        Assert.Equal(0, controller.PendingCommands);
    }

    [Fact]
    public void StoreRestoreRoundTripsIdentity() {
        var (controller, _, _) = Make();
        var state = controller.Store(Context.Empty);

        Assert.Equal(P1, state.Id);
        Assert.Equal(FactoryId, state.FactoryId);
    }

    [Fact]
    public void TickAnswersCommandsThroughTheWorldPort() {
        var (controller, world, router) = Make(withPlacement: true);
        world.Location = new FakeMachineLocation { Machines = { (6, 0) } };

        controller.Fire(new StartPeripheralEvent());
        controller.ReceiveDatagram(Incoming("{\"cid\":\"t1\",\"cmd\":\"list\"}"));
        controller.Fire(new TickPeripheralEvent(1));

        var (datagram, _) = Assert.Single(router.Delivered);
        Assert.Equal("c1", datagram.TargetAddress);
        var reply = JObject.Parse(datagram.Payload);
        Assert.Equal("t1", reply["re"]!.Value<string>());
        Assert.True(reply["ok"]!.Value<bool>());
    }

    [Fact]
    public void CommandsWithoutAWorldLocationFailWithNoPositionError() {
        var (controller, world, router) = Make(withPlacement: true);
        world.Location = null; // location not loaded

        controller.Fire(new StartPeripheralEvent());
        controller.ReceiveDatagram(Incoming("{\"cid\":\"t2\",\"cmd\":\"list\"}"));
        controller.Fire(new TickPeripheralEvent(1));

        var (datagram, _) = Assert.Single(router.Delivered);
        var reply = JObject.Parse(datagram.Payload);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Contains("no world position", reply["error"]!.Value<string>());
    }

    [Fact]
    public void BasicTierScansOnlyAdjacentTiles() {
        var (controller, world, router) = Make(withPlacement: true, tier: PeripheralTier.Basic);
        // Placement is at (5, 0). Machines at (6, 0) adjacent and (7, 0) one further.
        world.Location = new FakeMachineLocation { Machines = { (6, 0), (7, 0) } };

        controller.Fire(new StartPeripheralEvent());
        controller.ReceiveDatagram(Incoming("{\"cid\":\"t3\",\"cmd\":\"list\"}"));
        controller.Fire(new TickPeripheralEvent(1));

        var reply = JObject.Parse(router.Delivered[^1].Datagram.Payload);
        var members = (JArray) reply["data"]!["members"]!;
        Assert.Single(members);
        Assert.Equal(6, members[0]!["x"]!.Value<int>());
    }

    [Fact]
    public void ReadyTransitionPushesToSubscribers() {
        var (controller, world, router) = Make(withPlacement: true);
        var location = new FakeMachineLocation { Machines = { (6, 0) } };
        world.Location = location;

        controller.Fire(new StartPeripheralEvent());
        controller.ReceiveDatagram(Incoming("{\"cid\":\"s1\",\"cmd\":\"subscribe\",\"events\":[\"ready\"]}"));
        controller.Fire(new TickPeripheralEvent(1)); // subscription ack, machine not ready yet
        router.Delivered.Clear();

        location.Ready.Add((6, 0));
        controller.Fire(new TickPeripheralEvent(2)); // edge triggers exactly one push
        controller.Fire(new TickPeripheralEvent(3)); // still ready, no repeat push

        var (push, _) = Assert.Single(router.Delivered);
        var payload = JObject.Parse(push.Payload);
        Assert.Equal("machineReady", payload["event"]!.Value<string>());
        Assert.Equal(6, payload["machine"]!["x"]!.Value<int>());
    }
}
