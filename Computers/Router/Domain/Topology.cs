using Computers.Core;

namespace Computers.Router.Domain;

public record RouterNode(Id Id, string Location, int X, int Y, int? Channel, bool Enabled);

public record EndpointNode(Id Id, string Location, int X, int Y);

public interface INetworkTopology {
    ISet<Id> Neighbors(Id routerId);
    ISet<Id> EndpointsCoveredBy(Id routerId);
    ISet<Id> RoutersCovering(Id endpointId);
    Id? FindEndpointByAddress(string address);
}

public class RadiusChannelTopology : INetworkTopology {
    private readonly List<RouterNode> _routers;
    private readonly List<EndpointNode> _endpoints;
    private readonly double _coverageRadius;
    private readonly double _linkRadius;

    public RadiusChannelTopology(
        IEnumerable<RouterNode> routers,
        IEnumerable<EndpointNode> endpoints,
        double coverageRadius,
        double linkRadius
    ) {
        _routers = routers.ToList();
        _endpoints = endpoints.ToList();
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

    public ISet<Id> EndpointsCoveredBy(Id routerId) {
        var router = _routers.FirstOrDefault(r => r.Id == routerId);
        if (router is null || !router.Enabled) {
            return new HashSet<Id>();
        }

        return _endpoints
            .Where(endpoint => Covers(router, endpoint))
            .Select(endpoint => endpoint.Id)
            .ToHashSet();
    }

    public ISet<Id> RoutersCovering(Id endpointId) {
        var endpoint = _endpoints.FirstOrDefault(e => e.Id == endpointId);
        if (endpoint is null) {
            return new HashSet<Id>();
        }

        return _routers
            .Where(router => router.Enabled && Covers(router, endpoint))
            .Select(router => router.Id)
            .ToHashSet();
    }

    public Id? FindEndpointByAddress(string address) {
        return _endpoints.FirstOrDefault(endpoint => endpoint.Id.Last == address)?.Id;
    }

    private bool Covers(RouterNode router, EndpointNode endpoint) {
        return router.Location == endpoint.Location
            && Distance(router.X, router.Y, endpoint.X, endpoint.Y) <= _coverageRadius;
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
