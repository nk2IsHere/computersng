using Computers.Computer;
using Computers.Core;
using Computers.Peripheral.Domain;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Router.Domain.Wire;
using Computers.WeatherStation.Domain.Wire;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace Computers.WeatherStation.Domain;

public class WeatherStationStatefulDataContextEntry : PeripheralEntity<WeatherStationStatefulDataContextEntry> {

    private readonly IWeatherWorld _weatherWorld;
    private readonly PeripheralSubscriptions _subscriptions;
    private readonly WeatherStationCommandProcessor _processor;

    private WeatherReading? _reading;
    private (int Day, string Season, int Year)? _previousDay;
    private int? _previousTime;

    public WeatherStationStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        IWeatherWorld weatherWorld
    ) : base(factoryId, id, monitor, configuration, registry, routers) {
        _weatherWorld = weatherWorld;
        _subscriptions = new PeripheralSubscriptions(new[] { "day", "time" }, configuration.Peripheral.MaxSubscribers);
        _processor = new WeatherStationCommandProcessor(_subscriptions);
    }

    protected override void BeforeCommands() {
        var placement = Registry.PlacementOf(Id);
        _reading = placement is null ? null : _weatherWorld.ReadingFor(placement);
    }

    protected override Reply? ProcessRequest(string sourceAddress, JObject request) {
        return _processor.Process(sourceAddress, request, _reading);
    }

    protected override void AfterCommands() {
        if (_reading is null) {
            return;
        }

        var day = (_reading.Day, _reading.Season, _reading.Year);
        if (_previousDay is not null && day != _previousDay) {
            Push("day");
        }
        if (_previousTime is not null && _reading.Time != _previousTime) {
            Push("time");
        }

        _previousDay = day;
        _previousTime = _reading.Time;
    }

    private void Push(string eventName) {
        var subscribers = _subscriptions.Subscribers(eventName);
        if (subscribers.Count == 0) {
            return;
        }

        var push = WireJson.Serialize(new WeatherEvent(eventName, _reading!));
        foreach (var subscriber in subscribers) {
            SendTo(subscriber, push);
        }
    }
}
