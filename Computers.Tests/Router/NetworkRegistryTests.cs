using Computers.Computer;
using Computers.Core;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Tests.TestDoubles;
using Xunit;

namespace Computers.Tests.Router;

public class NetworkRegistryTests {
    private static readonly Id R1 = "router.r1".AsId();
    private static readonly Id R2 = "router.r2".AsId();
    private static readonly Id C1 = "computer.c1".AsId();

    private static Configuration TestConfiguration() => new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>()
            // LAN keys keep their spec defaults (10/16/16/256/65536)
        }
    };

    private static (NetworkRegistry Registry, List<FakeRouterPort> Ports) Make(params FakeRouterPort[] ports) {
        var portList = ports.ToList();
        var lookup = new ContextLookup<IRouterPort>(
            () => portList
                .Select(port => new ContextEntry<IRouterPort>(port.Id, port))
                .ToHashSet()
        );
        return (new NetworkRegistry(new TestMonitor(), TestConfiguration(), lookup), portList);
    }

    [Fact]
    public void RegisteredPlacementsFormTopology() {
        var (registry, _) = Make(new FakeRouterPort(R1));
        registry.Register(new Placement(R1, NetworkNodeKind.Router, "Farm", 0, 0));
        registry.Register(new Placement(C1, NetworkNodeKind.Computer, "Farm", 3, 0));
        Assert.Equal(new HashSet<Id> { R1 }, registry.RoutersCovering(C1));
    }

    [Fact]
    public void UnregisterRemovesNode() {
        var (registry, _) = Make(new FakeRouterPort(R1));
        registry.Register(new Placement(R1, NetworkNodeKind.Router, "Farm", 0, 0));
        registry.Register(new Placement(C1, NetworkNodeKind.Computer, "Farm", 3, 0));
        registry.Unregister(R1);
        Assert.Empty(registry.RoutersCovering(C1));
    }

    [Fact]
    public void ChannelChangesVisibleAfterInvalidate() {
        var portR1 = new FakeRouterPort(R1);
        var portR2 = new FakeRouterPort(R2);
        var (registry, _) = Make(portR1, portR2);
        registry.Register(new Placement(R1, NetworkNodeKind.Router, "Farm", 0, 0));
        registry.Register(new Placement(R2, NetworkNodeKind.Router, "Shed", 0, 0));
        Assert.Empty(registry.Neighbors(R1));

        portR1.Channel = 4;
        portR2.Channel = 4;
        registry.Invalidate();
        Assert.Equal(new HashSet<Id> { R2 }, registry.Neighbors(R1));
    }

    [Fact]
    public void RouterWithoutPortIsTreatedAsDisabled() {
        var (registry, _) = Make(); // no ports at all
        registry.Register(new Placement(R1, NetworkNodeKind.Router, "Farm", 0, 0));
        registry.Register(new Placement(C1, NetworkNodeKind.Computer, "Farm", 3, 0));
        Assert.Empty(registry.RoutersCovering(C1));
    }

    [Fact]
    public void ReplaceAllSwapsTheWholeWorld() {
        var (registry, _) = Make(new FakeRouterPort(R1));
        registry.Register(new Placement(C1, NetworkNodeKind.Computer, "Farm", 3, 0));
        registry.ReplaceAll(new[] {
            new Placement(R1, NetworkNodeKind.Router, "Shed", 0, 0),
            new Placement(C1, NetworkNodeKind.Computer, "Shed", 1, 0)
        });
        Assert.Equal(new HashSet<Id> { R1 }, registry.RoutersCovering(C1));
    }

    [Fact]
    public void ClearEmptiesEverything() {
        var (registry, _) = Make(new FakeRouterPort(R1));
        registry.Register(new Placement(R1, NetworkNodeKind.Router, "Farm", 0, 0));
        registry.Clear();
        Assert.Null(registry.FindComputerByAddress("c1"));
        Assert.Empty(registry.Neighbors(R1));
    }
}
