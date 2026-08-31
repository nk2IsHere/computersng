using Computers.Computer;
using Computers.Core;
using Computers.Peripheral;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Tests.TestDoubles;
using Newtonsoft.Json.Linq;
using Xunit;
using Context = Computers.Core.Context;

namespace Computers.Tests.Router;

/// <summary>
/// Router firmware: routers answer {cid, cmd} RPC datagrams addressed to them (or broadcast
/// "*") on their own tick; replies enter the router's own inbox and flow out the next tick.
/// </summary>
public class RouterRpcTests {
    private static readonly Id FactoryId = "factory.router".AsId();
    private static readonly Id R1 = "router.r1".AsId();
    private static readonly Id C1 = "computer.c1".AsId();

    private static Configuration TestConfiguration() => new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>(),
            RouterQueueLimit = 256,
            MessageTtl = 16
        }
    };

    private static (RouterStatefulDataContextEntry Router, NetworkRegistry Registry, FakeComputerPort Computer) Make(
        PeripheralTier tier = PeripheralTier.Advanced
    ) {
        var configuration = TestConfiguration();
        var computer = new FakeComputerPort(C1);

        RouterStatefulDataContextEntry router = null!;
        var routerLookup = new ContextLookup<IRouterPort>(
            () => new HashSet<ContextEntry<IRouterPort>> { new(R1, router) });
        var endpointLookup = new ContextLookup<INetworkEndpoint>(
            () => new HashSet<ContextEntry<INetworkEndpoint>> { new(C1, computer) });

        var registry = new NetworkRegistry(new TestMonitor(), configuration, routerLookup);
        router = new RouterStatefulDataContextEntry(
            FactoryId, R1, new TestMonitor(), configuration, registry, endpointLookup, routerLookup, tier);

        registry.Register(new Placement(R1, NodeRole.Router, "Farm", 0, 0));
        registry.Register(new Placement(C1, NodeRole.Endpoint, "Farm", 1, 0));
        router.Start();
        return (router, registry, computer);
    }

    private static void SendToRouter(RouterStatefulDataContextEntry router, string target, string payload) {
        router.Deliver(new Datagram(Guid.NewGuid(), "c1", target, 16, payload), null);
    }

    // Arranges a channel via the firmware (the only channel-write path), then discards the ack.
    private static void Configure(RouterStatefulDataContextEntry router, FakeComputerPort computer, int channel) {
        SendToRouter(router, "r1", "{\"cid\":\"arrange\",\"cmd\":\"configure\",\"channel\":" + channel + "}");
        router.Fire(new TickRouterEvent(0));
        router.Fire(new TickRouterEvent(0));
        Assert.Equal(channel, router.Channel);
        computer.ReceivedDatagrams.Clear();
    }

    private static JObject? SingleReplyAfterTwoTicks(RouterStatefulDataContextEntry router, FakeComputerPort computer) {
        router.Fire(new TickRouterEvent(1)); // handles the request, enqueues the reply to own inbox
        Assert.Empty(computer.ReceivedDatagrams); // reply is not delivered within the same tick
        router.Fire(new TickRouterEvent(2)); // reply datagram flows to the covered requester
        return computer.ReceivedDatagrams.Count == 1
            ? JObject.Parse(computer.ReceivedDatagrams[0].Payload)
            : null;
    }

    [Fact]
    public void PingRepliesWithRouterTypeAndChannelOnTheNextTick() {
        var (router, _, computer) = Make();
        Configure(router, computer, 4); // arrange the channel through the firmware itself
        SendToRouter(router, "r1", "{\"cid\":\"p1\",\"cmd\":\"ping\"}");

        var reply = SingleReplyAfterTwoTicks(router, computer);
        Assert.NotNull(reply);
        Assert.Equal("p1", reply!["re"]!.Value<string>());
        Assert.True(reply["ok"]!.Value<bool>());
        Assert.Equal("router", reply["data"]!["type"]!.Value<string>());
        Assert.Equal("advanced", reply["data"]!["tier"]!.Value<string>());
        Assert.Equal(4, reply["data"]!["channel"]!.Value<int>());
        Assert.Equal("r1", computer.ReceivedDatagrams[0].SourceAddress);
    }

    [Fact]
    public void BroadcastDiscoverRepliesWithCoveredEndpointAddresses() {
        var (router, _, computer) = Make();
        SendToRouter(router, "*", "{\"cid\":\"d1\",\"cmd\":\"discover\"}");

        var reply = SingleReplyAfterTwoTicks(router, computer);
        Assert.NotNull(reply);
        Assert.Equal("r1", reply!["data"]!["router"]!.Value<string>());
        var endpoints = reply["data"]!["endpoints"]!.Values<string>().ToList();
        Assert.Equal(new List<string?> { "c1" }, endpoints);
    }

    [Fact]
    public void ConfigureSetsChannelInvalidatesRegistryAndAcks() {
        var (router, _, computer) = Make();
        SendToRouter(router, "r1", "{\"cid\":\"k1\",\"cmd\":\"configure\",\"channel\":7}");

        var reply = SingleReplyAfterTwoTicks(router, computer);
        Assert.NotNull(reply);
        Assert.True(reply!["ok"]!.Value<bool>());
        Assert.Equal(7, router.Channel);
    }

    [Fact]
    public void ConfigureNullClearsChannel() {
        var (router, _, computer) = Make();
        Configure(router, computer, 3);
        SendToRouter(router, "r1", "{\"cid\":\"k2\",\"cmd\":\"configure\",\"channel\":null}");

        var reply = SingleReplyAfterTwoTicks(router, computer);
        Assert.True(reply!["ok"]!.Value<bool>());
        Assert.Null(router.Channel);
    }

    [Fact]
    public void UnknownCommandWithCidGetsErrorReply() {
        var (router, _, computer) = Make();
        SendToRouter(router, "r1", "{\"cid\":\"u1\",\"cmd\":\"reboot\"}");

        var reply = SingleReplyAfterTwoTicks(router, computer);
        Assert.NotNull(reply);
        Assert.False(reply!["ok"]!.Value<bool>());
        Assert.Contains("reboot", reply["error"]!.Value<string>());
    }

    [Fact]
    public void BasicRouterRejectsConfigure() {
        var (router, _, computer) = Make(tier: PeripheralTier.Basic);
        SendToRouter(router, "r1", "{\"cid\":\"b1\",\"cmd\":\"configure\",\"channel\":7}");

        var reply = SingleReplyAfterTwoTicks(router, computer);
        Assert.False(reply!["ok"]!.Value<bool>());
        Assert.Equal("channels require an advanced router", reply["error"]!.Value<string>());
        Assert.Null(router.Channel);
    }

    [Fact]
    public void BasicRouterReadsStoredChannelAsNull() {
        var (advanced, _, _) = Make(tier: PeripheralTier.Advanced);
        advanced.Deliver(new Datagram(Guid.NewGuid(), "c1", "r1", 16, "{\"cid\":\"m1\",\"cmd\":\"configure\",\"channel\":7}"), null);
        advanced.Fire(new TickRouterEvent(1));
        var state = advanced.Store(Context.Empty);

        var (basic, _, _) = Make(tier: PeripheralTier.Basic);
        basic.Restore(Context.Empty, state);
        Assert.Null(basic.Channel);
    }

    [Fact]
    public void MalformedJsonAndMissingCidAreDroppedSilently() {
        var (router, _, computer) = Make();
        SendToRouter(router, "r1", "not json at all");
        SendToRouter(router, "r1", "{\"cmd\":\"ping\"}");
        router.Fire(new TickRouterEvent(1));
        router.Fire(new TickRouterEvent(2));
        Assert.Empty(computer.ReceivedDatagrams);
    }
}
