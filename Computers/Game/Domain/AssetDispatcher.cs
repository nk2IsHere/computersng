using Computers.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Computers.Game.Domain;

public class AssetDispatcher: IEventHandler {

    private readonly IMonitor _monitor;
    private readonly ISet<ContextEntry<ILoaderService>> _loaders;
    private readonly ISet<ContextEntry<IPatcherService>> _patchers;
    
    public AssetDispatcher(IMonitor monitor, ISet<ContextEntry<ILoaderService>> loaders, ISet<ContextEntry<IPatcherService>> patchers) {
        _monitor = monitor;
        _loaders = loaders;
        _patchers = patchers;
        
        monitor.Log($"AssetDispatcher initialized with {loaders.Count} loaders and {patchers.Count} patchers.", LogLevel.Debug);
    }

    public ISet<Type> EventTypes => new HashSet<Type> { typeof(AssetRequestedEvent) };
    
    public void Handle(IEvent @event) {
        var eventArgs = @event.Data<AssetRequestedEventArgs>();
        
        foreach (var patcher in _patchers) {
            if (!patcher.Value.CanPatch(eventArgs.DataType, eventArgs.Name)) continue;
            eventArgs.Edit(patcher.Value.Patch);
            return;
        }
        
        foreach (var loader in _loaders) {
            if (!loader.Value.ShouldLoad(eventArgs.DataType, eventArgs.Name)) continue;
            eventArgs.LoadFrom(() => loader.Value.Load(eventArgs.Name), AssetLoadPriority.High);
            return;
        }
    }
}
