using Computers.Core;

namespace Computers.Router.Domain;

public enum NetworkNodeKind {
    Computer,
    Router
}

public record RouterNode(Id Id, string Location, int X, int Y, int? Channel, bool Enabled);

public record ComputerNode(Id Id, string Location, int X, int Y);

public interface INetworkTopology {
    ISet<Id> Neighbors(Id routerId);
    ISet<Id> ComputersCoveredBy(Id routerId);
    ISet<Id> RoutersCovering(Id computerId);
    Id? FindComputerByAddress(string address);
}

public static class NetworkTopologyExtensions {
    public static ISet<Id> ReachableComputers(this INetworkTopology topology, Id computerId) {
        var visited = new HashSet<Id>();
        var frontier = new Queue<Id>(topology.RoutersCovering(computerId));
        var computers = new HashSet<Id>();

        while (frontier.Count > 0) {
            var routerId = frontier.Dequeue();
            if (!visited.Add(routerId)) {
                continue;
            }

            computers.UnionWith(topology.ComputersCoveredBy(routerId));
            topology.Neighbors(routerId).ForEach(frontier.Enqueue);
        }

        computers.Remove(computerId);
        return computers;
    }
}

public class RadiusChannelTopology : INetworkTopology {
    private readonly List<RouterNode> _routers;
    private readonly List<ComputerNode> _computers;
    private readonly double _coverageRadius;
    private readonly double _linkRadius;

    public RadiusChannelTopology(
        IEnumerable<RouterNode> routers,
        IEnumerable<ComputerNode> computers,
        double coverageRadius,
        double linkRadius
    ) {
        _routers = routers.ToList();
        _computers = computers.ToList();
        _coverageRadius = coverageRadius;
        _linkRadius = linkRadius;
    }

    public ISet<Id> Neighbors(Id routerId) {
        var router = _routers.FirstOrDefault(r => r.Id == routerId);
        if (router is null || !router.Enabled) {
            return new HashSet<Id>();
        }

        return _routers
            .Where(other => other.Enabled && other.Id != router.Id)
            .Where(other => IsLinked(router, other))
            .Select(other => other.Id)
            .ToHashSet();
    }

    public ISet<Id> ComputersCoveredBy(Id routerId) {
        var router = _routers.FirstOrDefault(r => r.Id == routerId);
        if (router is null || !router.Enabled) {
            return new HashSet<Id>();
        }

        return _computers
            .Where(computer => Covers(router, computer))
            .Select(computer => computer.Id)
            .ToHashSet();
    }

    public ISet<Id> RoutersCovering(Id computerId) {
        var computer = _computers.FirstOrDefault(c => c.Id == computerId);
        if (computer is null) {
            return new HashSet<Id>();
        }

        return _routers
            .Where(router => router.Enabled && Covers(router, computer))
            .Select(router => router.Id)
            .ToHashSet();
    }

    public Id? FindComputerByAddress(string address) {
        return _computers.FirstOrDefault(computer => computer.Id.Last == address)?.Id;
    }

    private bool Covers(RouterNode router, ComputerNode computer) {
        return router.Location == computer.Location
            && Distance(router.X, router.Y, computer.X, computer.Y) <= _coverageRadius;
    }

    private bool IsLinked(RouterNode router, RouterNode other) {
        var shortRange = router.Location == other.Location
            && Distance(router.X, router.Y, other.X, other.Y) <= _linkRadius;
        var longRange = router.Channel is not null && router.Channel == other.Channel;
        return shortRange || longRange;
    }

    private static double Distance(int x1, int y1, int x2, int y2) {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
