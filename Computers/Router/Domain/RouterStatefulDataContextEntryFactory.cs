using Computers.Computer;
using Computers.Core;
using StardewModdingAPI;

namespace Computers.Router.Domain;

public class RouterStatefulDataContextEntryFactory : IStatefulDataContextEntryFactory {

    private readonly Id _baseRouterId;

    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<IComputerPort> _computers;
    private readonly ContextLookup<IRouterPort> _routers;

    public RouterStatefulDataContextEntryFactory(
        Id id,
        Id baseRouterId,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IComputerPort> computers,
        ContextLookup<IRouterPort> routers
    ) {
        FactoryId = id;
        _baseRouterId = baseRouterId;
        _monitor = monitor;
        _configuration = configuration;
        _registry = registry;
        _computers = computers;
        _routers = routers;
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
            _computers,
            _routers
        );
    }
}
