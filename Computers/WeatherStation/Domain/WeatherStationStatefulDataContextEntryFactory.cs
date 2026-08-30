using Computers.Computer;
using Computers.Core;
using Computers.Router;
using Computers.Router.Domain;
using StardewModdingAPI;

namespace Computers.WeatherStation.Domain;

public class WeatherStationStatefulDataContextEntryFactory : IStatefulDataContextEntryFactory {

    private readonly Id _basePeripheralId;

    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<IRouterPort> _routers;
    private readonly IWeatherWorld _weatherWorld;

    public WeatherStationStatefulDataContextEntryFactory(
        Id id,
        Id basePeripheralId,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        IWeatherWorld weatherWorld
    ) {
        FactoryId = id;
        _basePeripheralId = basePeripheralId;
        _monitor = monitor;
        _configuration = configuration;
        _registry = registry;
        _routers = routers;
        _weatherWorld = weatherWorld;
    }

    public Id FactoryId { get; }

    public IContextEntry ProduceValue() {
        return ProduceValue(ContextEntryState.Empty);
    }

    public IContextEntry ProduceValue(ContextEntryState state) {
        var id = state.Id ?? _basePeripheralId / Id.Random();
        return new WeatherStationStatefulDataContextEntry(
            FactoryId,
            id,
            _monitor,
            _configuration,
            _registry,
            _routers,
            _weatherWorld
        );
    }
}
