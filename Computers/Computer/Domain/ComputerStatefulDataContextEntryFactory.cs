using Computers.Core;
using Computers.Game;
using StardewModdingAPI;

namespace Computers.Computer.Domain;

public class ComputerStatefulDataContextEntryFactory : IStatefulDataContextEntryFactory {

    private readonly Id _baseComputerId;
    
    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly Random _random;
    private readonly IRedundantLoader _coreLibraryLoader;
    private readonly IRedundantLoader _assetLoader;
    private readonly IRedundantLoader _dataLoader;
    
    public ComputerStatefulDataContextEntryFactory(
        Id id, 
        Id baseComputerId,
        IMonitor monitor,
        Configuration configuration,
        Random random,
        IRedundantLoader coreLibraryLoader,
        IRedundantLoader assetLoader,
        IRedundantLoader dataLoader
    ) {
        FactoryId = id;
        _baseComputerId = baseComputerId;
        _monitor = monitor;
        _configuration = configuration;
        _random = random;
        _coreLibraryLoader = coreLibraryLoader;
        _assetLoader = assetLoader;
        _dataLoader = dataLoader;
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
            _random,
            _coreLibraryLoader,
            _assetLoader,
            _dataLoader
        );
    }
}
