using Computers.Computer;
using Computers.Core;
using StardewModdingAPI;

namespace Computers.Router.Domain;

public class RouterStatefulDataContextEntryFactory : IStatefulDataContextEntryFactory {
    
    private readonly Id _baseRouterId;
    
    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;

    public RouterStatefulDataContextEntryFactory(
        Id id, 
        Id baseRouterId, 
        IMonitor monitor,
        Configuration configuration
    ) {
        FactoryId = id;
        _baseRouterId = baseRouterId;
        _monitor = monitor;
        _configuration = configuration;
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
            _configuration
        );
    }
}
