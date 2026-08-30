using Computers.Core;
using Computers.Router.Domain;
using Xunit;

namespace Computers.Tests.Router;

public class RouterStepTests {
    private static readonly Id R1 = "router.r1".AsId();
    private static readonly Id R2 = "router.r2".AsId();
    private static readonly Id R3 = "router.r3".AsId();
    private static readonly Id C1 = "computer.c1".AsId();
    private static readonly Id C2 = "computer.c2".AsId();

    // Line: C1 -- R1 -- R2 -- R3 -- C2 (coverage 10, link 16)
    private static RadiusChannelTopology Line() => new(
        new[] {
            new RouterNode(R1, "Farm", 0, 0, null, true),
            new RouterNode(R2, "Farm", 15, 0, null, true),
            new RouterNode(R3, "Farm", 30, 0, null, true)
        },
        new[] {
            new EndpointNode(C1, "Farm", 1, 0),
            new EndpointNode(C2, "Farm", 31, 0)
        },
        coverageRadius: 10,
        linkRadius: 16
    );

    private static Datagram ToC2(int ttl = 16) =>
        new(Guid.NewGuid(), "c1", "c2", ttl, "hello");

    [Fact]
    public void DeliversWhenTargetIsCovered() {
        var datagram = ToC2();
        var result = RouterStep.Process(
            R3, new[] { new InboxEntry(datagram, R2) }, new SeenMessageCache(16), Line());
        Assert.Single(result.Deliveries);
        Assert.Equal(C2, result.Deliveries[0].EndpointId);
        Assert.Equal(datagram, result.Deliveries[0].Datagram);
        Assert.Empty(result.Forwards); // delivered -> not forwarded
    }

    [Fact]
    public void ForwardsToNeighborsExceptArrivalEdgeWithDecrementedTtl() {
        var datagram = ToC2(ttl: 5);
        var result = RouterStep.Process(
            R2, new[] { new InboxEntry(datagram, R1) }, new SeenMessageCache(16), Line());
        Assert.Empty(result.Deliveries);
        Assert.Single(result.Forwards);
        Assert.Equal(R3, result.Forwards[0].TargetRouterId);
        Assert.Equal(4, result.Forwards[0].Datagram.Ttl);
        Assert.Equal(datagram.MessageId, result.Forwards[0].Datagram.MessageId);
    }

    [Fact]
    public void SeedFromComputerForwardsToAllNeighbors() {
        // ArrivedFromRouterId null = seeded by a computer send
        var result = RouterStep.Process(
            R2, new[] { new InboxEntry(ToC2(), null) }, new SeenMessageCache(16), Line());
        Assert.Equal(
            new HashSet<Id> { R1, R3 },
            result.Forwards.Select(f => f.TargetRouterId).ToHashSet());
    }

    [Fact]
    public void DropsAlreadySeenDatagrams() {
        var seen = new SeenMessageCache(16);
        var datagram = ToC2();
        var first = RouterStep.Process(R2, new[] { new InboxEntry(datagram, R1) }, seen, Line());
        var second = RouterStep.Process(R2, new[] { new InboxEntry(datagram, R3) }, seen, Line());
        Assert.Single(first.Forwards);
        Assert.Empty(second.Forwards);
        Assert.Empty(second.Deliveries);
    }

    [Fact]
    public void DropsWhenTtlExhausted() {
        var result = RouterStep.Process(
            R2, new[] { new InboxEntry(ToC2(ttl: 1), R1) }, new SeenMessageCache(16), Line());
        Assert.Empty(result.Deliveries);
        Assert.Empty(result.Forwards);
    }

    [Fact]
    public void TtlOneCanStillBeDeliveredLocally() {
        var result = RouterStep.Process(
            R3, new[] { new InboxEntry(ToC2(ttl: 1), R2) }, new SeenMessageCache(16), Line());
        Assert.Single(result.Deliveries);
    }

    [Fact]
    public void UnknownTargetAddressFloodsUntilTtlDies() {
        var datagram = new Datagram(Guid.NewGuid(), "c1", "ghost", 3, "boo");
        var result = RouterStep.Process(
            R2, new[] { new InboxEntry(datagram, R1) }, new SeenMessageCache(16), Line());
        Assert.Empty(result.Deliveries);
        Assert.Single(result.Forwards); // keeps flooding; dies by TTL eventually
    }

    [Fact]
    public void UnicastToRouterAddressLandsInRouterInboundAndIsNotForwarded() {
        var datagram = new Datagram(Guid.NewGuid(), "c1", "r2", 16, "{\"cid\":\"x\",\"cmd\":\"ping\"}");
        var result = RouterStep.Process(
            R2, new[] { new InboxEntry(datagram, R1) }, new SeenMessageCache(16), Line());
        Assert.Equal(datagram, Assert.Single(result.RouterInbound));
        Assert.Empty(result.Forwards);
        Assert.Empty(result.Deliveries);
    }

    [Fact]
    public void BroadcastLandsInRouterInboundAndStillForwards() {
        var datagram = new Datagram(Guid.NewGuid(), "c1", "*", 16, "{\"cid\":\"x\",\"cmd\":\"discover\"}");
        var result = RouterStep.Process(
            R2, new[] { new InboxEntry(datagram, R1) }, new SeenMessageCache(16), Line());
        Assert.Single(result.RouterInbound);
        var forward = Assert.Single(result.Forwards); // to R3, not back to R1
        Assert.Equal(R3, forward.TargetRouterId);
        Assert.Equal(15, forward.Datagram.Ttl);
        Assert.Empty(result.Deliveries); // endpoints never receive broadcast
    }

    [Fact]
    public void BroadcastWithExhaustedTtlIsHandledLocallyButNotForwarded() {
        var datagram = new Datagram(Guid.NewGuid(), "c1", "*", 1, "{\"cid\":\"x\",\"cmd\":\"discover\"}");
        var result = RouterStep.Process(
            R2, new[] { new InboxEntry(datagram, R1) }, new SeenMessageCache(16), Line());
        Assert.Single(result.RouterInbound);
        Assert.Empty(result.Forwards);
    }

    [Fact]
    public void BroadcastSecondCopyIsDroppedByDedup() {
        var seen = new SeenMessageCache(16);
        var datagram = new Datagram(Guid.NewGuid(), "c1", "*", 16, "{\"cid\":\"x\",\"cmd\":\"discover\"}");
        var first = RouterStep.Process(R2, new[] { new InboxEntry(datagram, R1) }, seen, Line());
        var second = RouterStep.Process(R2, new[] { new InboxEntry(datagram, R3) }, seen, Line());
        Assert.Single(first.RouterInbound);
        Assert.Empty(second.RouterInbound);
        Assert.Empty(second.Forwards);
    }

    [Fact]
    public void UnicastToAnotherRouterAddressIsForwardedNotHandled() {
        // Addressed to r3 but sitting in r2's inbox: r2 must flood it onward like any unknown-endpoint target.
        var datagram = new Datagram(Guid.NewGuid(), "c1", "r3", 16, "{\"cid\":\"x\",\"cmd\":\"ping\"}");
        var result = RouterStep.Process(
            R2, new[] { new InboxEntry(datagram, R1) }, new SeenMessageCache(16), Line());
        Assert.Empty(result.RouterInbound);
        Assert.Single(result.Forwards);
    }

    [Fact]
    public void EndToEndFloodSimulationDeliversExactlyOnceOnDiamond() {
        // Diamond: C1 -- R1 -- {R2 (top), R3 (bottom)} -- R4 -- C2; dedup at R4
        var r4 = "router.r4".AsId();
        var topology = new RadiusChannelTopology(
            new[] {
                new RouterNode(R1, "Farm", 0, 0, null, true),
                new RouterNode(R2, "Farm", 10, 10, null, true),
                new RouterNode(R3, "Farm", 10, -10, null, true),
                new RouterNode(r4, "Farm", 20, 0, null, true)
            },
            new[] {
                new EndpointNode(C1, "Farm", 0, 1),
                new EndpointNode(C2, "Farm", 20, 1)
            },
            coverageRadius: 5,
            linkRadius: 15
        );

        // Simulate ticks: per-router inbox + seen cache, until quiescent.
        var inboxes = new Dictionary<Id, List<InboxEntry>> {
            [R1] = new() { new InboxEntry(ToC2(), null) },
            [R2] = new(), [R3] = new(), [r4] = new()
        };
        var seenCaches = inboxes.Keys.ToDictionary(id => id, _ => new SeenMessageCache(16));
        var deliveries = new List<Delivery>();

        for (var tick = 0; tick < 10; tick++) {
            var nextInboxes = inboxes.Keys.ToDictionary(id => id, _ => new List<InboxEntry>());
            foreach (var (routerId, inbox) in inboxes) {
                var result = RouterStep.Process(routerId, inbox, seenCaches[routerId], topology);
                deliveries.AddRange(result.Deliveries);
                foreach (var forward in result.Forwards) {
                    nextInboxes[forward.TargetRouterId].Add(new InboxEntry(forward.Datagram, routerId));
                }
            }
            inboxes = nextInboxes;
        }

        Assert.Single(deliveries); // router-side dedup collapses the diamond
        Assert.Equal(C2, deliveries[0].EndpointId);
    }
}
