using Computers.Core;
using Computers.Router.Domain;
using Xunit;

namespace Computers.Tests.Router;

public class TopologyTests {
    private static readonly Id R1 = "router.r1".AsId();
    private static readonly Id R2 = "router.r2".AsId();
    private static readonly Id R3 = "router.r3".AsId();
    private static readonly Id C1 = "computer.c1".AsId();
    private static readonly Id C2 = "computer.c2".AsId();

    private static RadiusChannelTopology Build(
        IEnumerable<RouterNode> routers,
        IEnumerable<ComputerNode> computers
    ) => new(routers, computers, coverageRadius: 10, linkRadius: 16);

    [Fact]
    public void RouterCoversComputerWithinRadiusInSameLocation() {
        var topology = Build(
            new[] { new RouterNode(R1, "Farm", 0, 0, null, true) },
            new[] {
                new ComputerNode(C1, "Farm", 6, 8),   // dist 10 -> covered (inclusive)
                new ComputerNode(C2, "Farm", 11, 0)   // dist 11 -> not covered
            }
        );
        Assert.Equal(new HashSet<Id> { C1 }, topology.ComputersCoveredBy(R1));
        Assert.Equal(new HashSet<Id> { R1 }, topology.RoutersCovering(C1));
        Assert.Empty(topology.RoutersCovering(C2));
    }

    [Fact]
    public void CoverageDoesNotCrossLocations() {
        var topology = Build(
            new[] { new RouterNode(R1, "Farm", 0, 0, null, true) },
            new[] { new ComputerNode(C1, "Shed", 0, 0) }
        );
        Assert.Empty(topology.RoutersCovering(C1));
    }

    [Fact]
    public void RoutersLinkByRadiusInSameLocationOnly() {
        var topology = Build(
            new[] {
                new RouterNode(R1, "Farm", 0, 0, null, true),
                new RouterNode(R2, "Farm", 16, 0, null, true),  // dist 16 -> linked
                new RouterNode(R3, "Shed", 0, 0, null, true)    // other location, no channel -> not linked
            },
            Array.Empty<ComputerNode>()
        );
        Assert.Equal(new HashSet<Id> { R2 }, topology.Neighbors(R1));
        Assert.Equal(new HashSet<Id> { R1 }, topology.Neighbors(R2));
        Assert.Empty(topology.Neighbors(R3));
    }

    [Fact]
    public void MatchingChannelsLinkAcrossLocations() {
        var topology = Build(
            new[] {
                new RouterNode(R1, "Farm", 0, 0, 4, true),
                new RouterNode(R2, "Shed", 0, 0, 4, true),
                new RouterNode(R3, "Town", 0, 0, 5, true) // different channel
            },
            Array.Empty<ComputerNode>()
        );
        Assert.Equal(new HashSet<Id> { R2 }, topology.Neighbors(R1));
        Assert.Empty(topology.Neighbors(R3));
    }

    [Fact]
    public void DisabledRoutersHaveNoLinksAndNoCoverage() {
        var topology = Build(
            new[] {
                new RouterNode(R1, "Farm", 0, 0, 4, false),
                new RouterNode(R2, "Farm", 5, 0, 4, true)
            },
            new[] { new ComputerNode(C1, "Farm", 1, 0) }
        );
        Assert.Empty(topology.Neighbors(R1));
        Assert.Empty(topology.ComputersCoveredBy(R1));
        Assert.Equal(new HashSet<Id> { R2 }, topology.RoutersCovering(C1));
        Assert.Empty(topology.Neighbors(R2)); // R1 disabled, so not a neighbor of R2 either
    }

    [Fact]
    public void FindComputerByAddressMatchesLastSegment() {
        var topology = Build(
            Array.Empty<RouterNode>(),
            new[] { new ComputerNode(C1, "Farm", 0, 0) }
        );
        Assert.Equal(C1, topology.FindComputerByAddress("c1"));
        Assert.Null(topology.FindComputerByAddress("nope"));
    }

    [Fact]
    public void ReachableComputersCrossesChannelBridge() {
        // C1 -- R1(Farm, ch 4) ~~~ R2(Shed, ch 4) -- C2
        var topology = Build(
            new[] {
                new RouterNode(R1, "Farm", 0, 0, 4, true),
                new RouterNode(R2, "Shed", 0, 0, 4, true)
            },
            new[] {
                new ComputerNode(C1, "Farm", 1, 0),
                new ComputerNode(C2, "Shed", 1, 0)
            }
        );
        Assert.Equal(new HashSet<Id> { C2 }, topology.ReachableComputers(C1));
        Assert.Equal(new HashSet<Id> { C1 }, topology.ReachableComputers(C2));
    }

    [Fact]
    public void ReachableComputersExcludesSelfAndOffline() {
        var topology = Build(
            new[] { new RouterNode(R1, "Farm", 0, 0, null, true) },
            new[] {
                new ComputerNode(C1, "Farm", 1, 0),
                new ComputerNode(C2, "Farm", 50, 50) // offline
            }
        );
        Assert.Empty(topology.ReachableComputers(C1)); // only itself covered -> empty
        Assert.Empty(topology.ReachableComputers(C2)); // offline -> empty
    }
}
