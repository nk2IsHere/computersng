using Computers.Core;
using Computers.Game;
using Computers.Router;
using Computers.Router.Domain;
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
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<IRouterPort> _routers;
    private readonly ComputerScheduler _scheduler;

    public ComputerStatefulDataContextEntryFactory(
        Id id,
        Id baseComputerId,
        IMonitor monitor,
        Configuration configuration,
        Random random,
        IRedundantLoader coreLibraryLoader,
        IRedundantLoader assetLoader,
        IRedundantLoader dataLoader,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        ComputerScheduler scheduler
    ) {
        FactoryId = id;
        _baseComputerId = baseComputerId;
        _monitor = monitor;
        _configuration = configuration;
        _random = random;
        _coreLibraryLoader = coreLibraryLoader;
        _assetLoader = assetLoader;
        _dataLoader = dataLoader;
        _registry = registry;
        _routers = routers;
        _scheduler = scheduler;
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
            _dataLoader,
            _registry,
            _routers,
            _scheduler
        );
    }
}
