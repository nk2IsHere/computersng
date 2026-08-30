using Computers.Computer;
using Computers.Core;
using Computers.Peripheral;
using Computers.Router;
using Computers.Router.Domain;
using StardewModdingAPI;

namespace Computers.MachineController.Domain;

public class MachineControllerStatefulDataContextEntryFactory : IStatefulDataContextEntryFactory {

    private readonly Id _basePeripheralId;

    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<IRouterPort> _routers;
    private readonly IMachineWorld _machineWorld;
    private readonly PeripheralTier _tier;

    public MachineControllerStatefulDataContextEntryFactory(
        Id id,
        Id basePeripheralId,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        IMachineWorld machineWorld,
        PeripheralTier tier
    ) {
        FactoryId = id;
        _basePeripheralId = basePeripheralId;
        _monitor = monitor;
        _configuration = configuration;
        _registry = registry;
        _routers = routers;
        _machineWorld = machineWorld;
        _tier = tier;
    }

    public Id FactoryId { get; }

    public IContextEntry ProduceValue() {
        return ProduceValue(ContextEntryState.Empty);
    }

    public IContextEntry ProduceValue(ContextEntryState state) {
        var id = state.Id ?? _basePeripheralId / Id.Random();
        return new MachineControllerStatefulDataContextEntry(
            FactoryId,
            id,
            _monitor,
            _configuration,
            _registry,
            _routers,
            _machineWorld,
            _tier
        );
    }
}
