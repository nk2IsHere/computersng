using Computers.Computer;
using Computers.Computer.Domain.Api;
using Computers.Core;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Tests.TestDoubles;
using Xunit;

namespace Computers.Tests.Computer;

public class NetworkComputerApiTests {
    private static readonly Id R1 = "router.r1".AsId();
    private static readonly Id C1 = "computer.c1".AsId();
    private static readonly Id C2 = "computer.c2".AsId();

    private static (NetworkComputerState State, NetworkRegistry Registry, FakeRouterPort Router) Make(
        bool computerInRange = true
    ) {
        var computerPort = new FakeComputerPort(C1);
        var routerPort = new FakeRouterPort(R1);
        var routerLookup = new ContextLookup<IRouterPort>(
            () => new HashSet<ContextEntry<IRouterPort>> { new(R1, routerPort) });
        var registry = new NetworkRegistry(new TestMonitor(), computerPort.Configuration, routerLookup);

        registry.Register(new Placement(R1, NetworkNodeKind.Router, "Farm", 0, 0));
        registry.Register(new Placement(C1, NetworkNodeKind.Computer, "Farm", computerInRange ? 1 : 99, 0));
        registry.Register(new Placement(C2, NetworkNodeKind.Computer, "Farm", 2, 0));

        var api = new NetworkComputerApi(computerPort, registry, routerLookup);
        return ((NetworkComputerState) api.Api, registry, routerPort);
    }

    [Fact]
    public void SendMessageSeedsAllCoveringRouters() {
        var (state, _, router) = Make();
        state.SendMessage("c2", "hello");
        var (datagram, from) = Assert.Single(router.Delivered);
        Assert.Equal("c1", datagram.SourceAddress);
        Assert.Equal("c2", datagram.TargetAddress);
        Assert.Equal("hello", datagram.Payload);
        Assert.Equal(16, datagram.Ttl); // spec default
        Assert.Null(from);
    }

    [Fact]
    public void SendMessageThrowsWhenOffline() {
        var (state, _, _) = Make(computerInRange: false);
        var exception = Assert.Throws<InvalidOperationException>(() => state.SendMessage("c2", "hello"));
        Assert.Contains("offline", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SendMessageThrowsWhenPayloadTooLarge() {
        var (state, _, _) = Make();
        var oversized = new string('x', 65537);
        Assert.Throws<InvalidOperationException>(() => state.SendMessage("c2", oversized));
    }

    [Fact]
    public void GetAddressReturnsLastSegment() {
        var (state, _, _) = Make();
        Assert.Equal("c1", state.GetAddress());
    }

    [Fact]
    public void ListReachableReturnsOtherComputers() {
        var (state, _, _) = Make();
        Assert.Equal(new[] { "c2" }, state.ListReachable());
    }

    [Fact]
    public void GetRoutersReportsAddressAndChannel() {
        var (state, _, router) = Make();
        router.Channel = 9;
        var routers = state.GetRouters();
        var entry = Assert.Single(routers);
        Assert.Equal("r1", entry["address"]);
        Assert.Equal(9, entry["channel"]);
    }

    [Fact]
    public void ConfigureRouterSetsChannelAndInvalidates() {
        var (state, registry, router) = Make();
        state.ConfigureRouter("r1", 3);
        Assert.Equal(3, router.Channel);
        state.ConfigureRouter("r1", null);
        Assert.Null(router.Channel);
    }

    [Fact]
    public void ConfigureRouterThrowsForNonCoveringRouter() {
        var (state, _, _) = Make(computerInRange: false);
        Assert.Throws<InvalidOperationException>(() => state.ConfigureRouter("r1", 3));
    }
}
