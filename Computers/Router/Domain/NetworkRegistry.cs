using Computers.Computer;
using Computers.Core;
using StardewModdingAPI;

namespace Computers.Router.Domain;

public enum NodeRole {
    Router,
    Endpoint
}

public record Placement(Id Id, NodeRole Role, string Location, int X, int Y);

public class NetworkRegistry : INetworkTopology {
    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly ContextLookup<IRouterPort> _routers;

    private readonly object _lock = new();
    private readonly Dictionary<Id, Placement> _placements = new();
    private RadiusChannelTopology? _topology;

    public NetworkRegistry(
        IMonitor monitor,
        Configuration configuration,
        ContextLookup<IRouterPort> routers
    ) {
        _monitor = monitor;
        _configuration = configuration;
        _routers = routers;
    }

    public void Register(Placement placement) {
        lock (_lock) {
            _placements[placement.Id] = placement;
            _topology = null;
        }
    }

    public void Unregister(Id id) {
        lock (_lock) {
            _placements.Remove(id);
            _topology = null;
        }
    }

    public void ReplaceAll(IEnumerable<Placement> placements) {
        lock (_lock) {
            _placements.Clear();
            placements.ForEach(placement => _placements[placement.Id] = placement);
            _topology = null;
        }
    }

    public void Clear() {
        lock (_lock) {
            _placements.Clear();
            _topology = null;
        }
    }

    public void Invalidate() {
        lock (_lock) {
            _topology = null;
        }
    }

    public Placement? PlacementOf(Id id) {
        lock (_lock) {
            return _placements.GetValueOrDefault(id);
        }
    }

    public ISet<Id> Neighbors(Id routerId) {
        lock (_lock) {
            return Topology().Neighbors(routerId);
        }
    }

    public ISet<Id> EndpointsCoveredBy(Id routerId) {
        lock (_lock) {
            return Topology().EndpointsCoveredBy(routerId);
        }
    }

    public ISet<Id> RoutersCovering(Id endpointId) {
        lock (_lock) {
            return Topology().RoutersCovering(endpointId);
        }
    }

    public Id? FindEndpointByAddress(string address) {
        lock (_lock) {
            return Topology().FindEndpointByAddress(address);
        }
    }

    private RadiusChannelTopology Topology() {
        if (_topology is not null) {
            return _topology;
        }

        var routerPorts = _routers
            .Get()
            .ToDictionary(entry => entry.Id, entry => entry.Value);

        var routerNodes = new List<RouterNode>();
        var endpointNodes = new List<EndpointNode>();

        foreach (var placement in _placements.Values) {
            if (placement.Role != NodeRole.Router) {
                endpointNodes.Add(new EndpointNode(placement.Id, placement.Location, placement.X, placement.Y));
                continue;
            }

            var port = routerPorts.GetValueOrDefault(placement.Id);
            routerNodes.Add(new RouterNode(
                placement.Id,
                placement.Location,
                placement.X,
                placement.Y,
                port?.Channel,
                port?.IsEnabled ?? false
            ));
        }

        return _topology = new RadiusChannelTopology(
            routerNodes,
            endpointNodes,
            _configuration.Network.CoverageRadius,
            _configuration.Network.LinkRadius
        );
    }
}
