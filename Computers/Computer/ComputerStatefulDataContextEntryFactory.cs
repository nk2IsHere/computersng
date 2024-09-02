using Computers.Computer.Boundary;
using Computers.Core;
using Computers.Game.Boundary;
using StardewModdingAPI;

namespace Computers.Computer;

public class ComputerStatefulDataContextEntryFactory : IStatefulDataContextEntryFactory {
    
    private readonly string _computerIdPrefix;
    
    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly IRedundantLoader _coreLibraryLoader;
    private readonly IRedundantLoader _assetLoader;
    
    public ComputerStatefulDataContextEntryFactory(
        string id, 
        string computerIdPrefix,
        IMonitor monitor,
        Configuration configuration,
        IRedundantLoader coreLibraryLoader,
        IRedundantLoader assetLoader
    ) {
        FactoryId = id;
        _computerIdPrefix = computerIdPrefix;
        _monitor = monitor;
        _configuration = configuration;
        _coreLibraryLoader = coreLibraryLoader;
        _assetLoader = assetLoader;
    }

    public string FactoryId { get; }
    
    public IContextEntry ProduceValue() {
        return ProduceValue(ContextEntryState.Empty);
    }

    public IContextEntry ProduceValue(ContextEntryState state) {
        var id = state.Id ?? $"{_computerIdPrefix}.{Guid.NewGuid().ToString()}";
        return new ComputerStatefulDataContextEntry(
            FactoryId,
            id,
            _monitor,
            _configuration,
            _coreLibraryLoader,
            _assetLoader
        );
    }
}
