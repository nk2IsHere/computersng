using Computers.Core;
using Computers.Game;
using StardewModdingAPI;

namespace Computers.Computer.Domain;

public class ComputerStatefulDataContextEntryFactory : IStatefulDataContextEntryFactory {

    private readonly Id _baseComputerId;
    
    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly IRedundantLoader _coreLibraryLoader;
    private readonly IRedundantLoader _assetLoader;
    
    public ComputerStatefulDataContextEntryFactory(
        Id id, 
        Id baseComputerId,
        IMonitor monitor,
        Configuration configuration,
        IRedundantLoader coreLibraryLoader,
        IRedundantLoader assetLoader
    ) {
        FactoryId = id;
        _baseComputerId = baseComputerId;
        _monitor = monitor;
        _configuration = configuration;
        _coreLibraryLoader = coreLibraryLoader;
        _assetLoader = assetLoader;
    }

    public Id FactoryId { get; }
    
    public IContextEntry ProduceValue() {
        return ProduceValue(ContextEntryState.Empty);
    }

    public IContextEntry ProduceValue(ContextEntryState state) {
        var id = state.Id ?? _baseComputerId / Id.Random();
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
