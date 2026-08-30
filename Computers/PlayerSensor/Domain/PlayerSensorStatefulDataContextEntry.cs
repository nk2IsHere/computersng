using Computers.Computer;
using Computers.Core;
using Computers.Peripheral.Domain;
using Computers.PlayerSensor.Domain.Wire;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace Computers.PlayerSensor.Domain;

public class PlayerSensorStatefulDataContextEntry : PeripheralEntity<PlayerSensorStatefulDataContextEntry> {

    private readonly ISensorWorld _sensorWorld;
    private readonly PeripheralSubscriptions _subscriptions;
    private readonly PlayerSensorCommandProcessor _processor;

    private SensorReading? _reading;
    private Dictionary<(string Kind, string Name), SensedCharacter>? _previousPresent;

    public PlayerSensorStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        ISensorWorld sensorWorld
    ) : base(factoryId, id, monitor, configuration, registry, routers) {
        _sensorWorld = sensorWorld;
        _subscriptions = new PeripheralSubscriptions(new[] { "presence" }, configuration.Peripheral.MaxSubscribers);
        _processor = new PlayerSensorCommandProcessor(_subscriptions);
    }

    protected override void BeforeCommands() {
        var placement = Registry.PlacementOf(Id);
        _reading = placement is null
            ? null
            : _sensorWorld.ReadingFor(placement, Configuration.PlayerSensor.Radius);
    }

    protected override Reply? ProcessRequest(string sourceAddress, JObject request) {
        return _processor.Process(sourceAddress, request, _reading);
    }

    protected override void AfterCommands() {
        if (_reading is null) {
            return;
        }

        var present = _reading.Players.Concat(_reading.Npcs)
            .ToDictionary(character => (character.Kind, character.Name));

        // The first reading is the baseline and pushes nothing.
        if (_previousPresent is not null) {
            var entered = present.Keys.Except(_previousPresent.Keys).Select(key => present[key]).ToList();
            var left = _previousPresent.Keys.Except(present.Keys).Select(key => _previousPresent[key]).ToList();

            if (entered.Count > 0 || left.Count > 0) {
                Push(new PresenceEvent("presence", entered, left));
            }
        }

        _previousPresent = present;
    }

    private void Push(PresenceEvent presenceEvent) {
        var subscribers = _subscriptions.Subscribers("presence");
        if (subscribers.Count == 0) {
            return;
        }

        var push = WireJson.Serialize(presenceEvent);
        foreach (var subscriber in subscribers) {
            SendTo(subscriber, push);
        }
    }
}
