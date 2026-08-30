using Computers.Computer;
using Computers.Core;
using Computers.Peripheral;
using StardewModdingAPI;

namespace Computers.Router.Domain;

public class RouterStatefulDataContextEntryFactory : IStatefulDataContextEntryFactory {

    private readonly Id _baseRouterId;

    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<INetworkEndpoint> _endpoints;
    private readonly ContextLookup<IRouterPort> _routers;
    private readonly PeripheralTier _tier;

    public RouterStatefulDataContextEntryFactory(
        Id id,
        Id baseRouterId,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<INetworkEndpoint> endpoints,
        ContextLookup<IRouterPort> routers,
        PeripheralTier tier
    ) {
        FactoryId = id;
        _baseRouterId = baseRouterId;
        _monitor = monitor;
        _configuration = configuration;
        _registry = registry;
        _endpoints = endpoints;
        _routers = routers;
        _tier = tier;
    }

    public Id FactoryId { get; }

    public IContextEntry ProduceValue() {
        return ProduceValue(ContextEntryState.Empty);
    }

    public IContextEntry ProduceValue(ContextEntryState state) {
        var id = state.Id ?? _baseRouterId / Id.Random();
        return new RouterStatefulDataContextEntry(
            FactoryId,
            id,
            _monitor,
            _configuration,
            _registry,
            _endpoints,
            _routers,
            _tier
        );
    }
}
