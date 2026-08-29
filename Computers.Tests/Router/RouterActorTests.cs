using Computers.Computer;
using Computers.Core;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Tests.TestDoubles;
using Xunit;
using Context = Computers.Core.Context;

namespace Computers.Tests.Router;

public class RouterActorTests {
    private static readonly Id FactoryId = "factory.router".AsId();
    private static readonly Id R1 = "router.r1".AsId();
    private static readonly Id C1 = "computer.c1".AsId();

    private static Configuration TestConfiguration(int queueLimit = 256) => new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>(),
            RouterQueueLimit = queueLimit
        }
    };

    private static (RouterStatefulDataContextEntry Router, NetworkRegistry Registry, FakeComputerPort Computer) Make(
        int queueLimit = 256
    ) {
        var configuration = TestConfiguration(queueLimit);
        var computer = new FakeComputerPort(C1);

        RouterStatefulDataContextEntry router = null!;
        var routerLookup = new ContextLookup<IRouterPort>(
            () => new HashSet<ContextEntry<IRouterPort>> { new(R1, router) });
        var computerLookup = new ContextLookup<IComputerPort>(
            () => new HashSet<ContextEntry<IComputerPort>> { new(C1, computer) });

        var registry = new NetworkRegistry(new TestMonitor(), configuration, routerLookup);
        router = new RouterStatefulDataContextEntry(
            FactoryId, R1, new TestMonitor(), configuration, registry, computerLookup, routerLookup);

        registry.Register(new Placement(R1, NetworkNodeKind.Router, "Farm", 0, 0));
        registry.Register(new Placement(C1, NetworkNodeKind.Computer, "Farm", 1, 0));
        return (router, registry, computer);
    }

    [Fact]
    public void DeliversDatagramToCoveredComputerOnTick() {
        var (router, _, computer) = Make();
        router.Start();
        router.Deliver(new Datagram(Guid.NewGuid(), "far", "c1", 16, "hi"), null);
        router.Fire(new TickRouterEvent(1));

        var networkEvent = Assert.IsType<NetworkMessageComputerEvent>(Assert.Single(computer.FiredEvents));
        Assert.Equal(C1, networkEvent.ComputerId);
        Assert.Equal("far", networkEvent.SourceAddress);
        Assert.Equal("hi", networkEvent.Payload);
    }

    [Fact]
    public void DisabledRouterDropsDeliveries() {
        var (router, _, computer) = Make();
        router.Deliver(new Datagram(Guid.NewGuid(), "far", "c1", 16, "hi"), null); // not started
        router.Start();
        router.Fire(new TickRouterEvent(1));
        Assert.Empty(computer.FiredEvents);
    }

    [Fact]
    public void InboxOverflowDropsExcess() {
        var (router, _, computer) = Make(queueLimit: 2);
        router.Start();
        for (var i = 0; i < 5; i++) {
            router.Deliver(new Datagram(Guid.NewGuid(), "far", "c1", 16, $"m{i}"), null);
        }
        router.Fire(new TickRouterEvent(1));
        Assert.Equal(2, computer.FiredEvents.Count);
    }

    [Fact]
    public void StopClearsInbox() {
        var (router, _, computer) = Make();
        router.Start();
        router.Deliver(new Datagram(Guid.NewGuid(), "far", "c1", 16, "hi"), null);
        router.Stop();
        router.Start();
        router.Fire(new TickRouterEvent(1));
        Assert.Empty(computer.FiredEvents);
    }

    [Fact]
    public void ChannelSurvivesStoreRestore() {
        var (router, _, _) = Make();
        router.Channel = 7;
        var state = router.Store(Context.Empty);

        var (fresh, _, _) = Make();
        fresh.Restore(Context.Empty, state);
        Assert.Equal(7, fresh.Channel);
    }
}
