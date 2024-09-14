using Computers.Computer;
using Computers.Core;
using StardewModdingAPI;
using Context = Computers.Core.Context;

namespace Computers.Router.Domain;

public class RouterStatefulDataContextEntry : IContextEntry.StatefulDataContextEntry<RouterStatefulDataContextEntry>, IRouterPort {
    private readonly IMonitor _monitor;
    
    public RouterStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration
    ) : base(factoryId, id) {
        _monitor = monitor;
        Configuration = configuration;
    }

    public override object GetValue(Context context) {
        return this;
    }

    public override void Restore(Context context, ContextEntryState state) {
    }

    public override ContextEntryState Store(Context context) {
        var state = ContextEntryState.Empty;
        
        state.Id = Id;
        state.FactoryId = FactoryId;
        
        return state;
    }

    public Configuration Configuration { get; }

    public void Start() {
        _monitor.Log($"Router {Id} started");
    }

    public void Stop() {
    }
}
