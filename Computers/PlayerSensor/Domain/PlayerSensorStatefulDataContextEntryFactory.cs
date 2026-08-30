using Computers.Computer;
using Computers.Core;
using Computers.Router;
using Computers.Router.Domain;
using StardewModdingAPI;

namespace Computers.PlayerSensor.Domain;

public class PlayerSensorStatefulDataContextEntryFactory : IStatefulDataContextEntryFactory {

    private readonly Id _basePeripheralId;

    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<IRouterPort> _routers;
    private readonly ISensorWorld _sensorWorld;

    public PlayerSensorStatefulDataContextEntryFactory(
        Id id,
        Id basePeripheralId,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        ISensorWorld sensorWorld
    ) {
        FactoryId = id;
        _basePeripheralId = basePeripheralId;
        _monitor = monitor;
        _configuration = configuration;
        _registry = registry;
        _routers = routers;
        _sensorWorld = sensorWorld;
    }

    public Id FactoryId { get; }

    public IContextEntry ProduceValue() {
        return ProduceValue(ContextEntryState.Empty);
    }

    public IContextEntry ProduceValue(ContextEntryState state) {
        var id = state.Id ?? _basePeripheralId / Id.Random();
        return new PlayerSensorStatefulDataContextEntry(
            FactoryId,
            id,
            _monitor,
            _configuration,
            _registry,
            _routers,
            _sensorWorld
        );
    }
}
