using Computers.Computer;
using Computers.Core;
using Computers.Peripheral;
using Computers.Peripheral.Domain;
using Computers.PlayerSensor.Domain.Wire;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using Context = Computers.Core.Context;

namespace Computers.PlayerSensor.Domain;

public static class SensorTiers {
    public static int MaxRadius(PeripheralTier tier) => tier == PeripheralTier.Advanced ? 64 : 8;
}

public class PlayerSensorStatefulDataContextEntry : PeripheralEntity<PlayerSensorStatefulDataContextEntry> {

    private readonly ISensorWorld _sensorWorld;
    private readonly PeripheralTier _tier;
    private readonly PeripheralSubscriptions _subscriptions;
    private readonly PlayerSensorCommandProcessor _processor;

    private SensorReading? _reading;
    private Dictionary<(string Kind, string Name), SensedCharacter>? _previousPresent;

    private int? _configuredRadius;

    public PlayerSensorStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        ISensorWorld sensorWorld,
        PeripheralTier tier
    ) : base(factoryId, id, monitor, configuration, registry, routers) {
        _sensorWorld = sensorWorld;
        _tier = tier;
        _subscriptions = new PeripheralSubscriptions(new[] { "presence" }, configuration.Peripheral.MaxSubscribers);
        _processor = new PlayerSensorCommandProcessor(_subscriptions, ConfigureRadius, tier);
    }

    private void ConfigureRadius(int radius) {
        var cap = SensorTiers.MaxRadius(_tier);
        if (radius > cap) {
            throw new PeripheralRequestException($"radius must be between 1 and {cap} for this tier");
        }
        _configuredRadius = radius;
    }

    public int Radius => Math.Min(_configuredRadius ?? Configuration.PlayerSensor.Radius, SensorTiers.MaxRadius(_tier));

    public override void Restore(Context context, ContextEntryState state) {
        var radius = state.GetOrDefault<object?>("Radius", null);
        _configuredRadius = radius is null ? null : Convert.ToInt32(radius);
    }

    public override ContextEntryState Store(Context context) {
        var state = base.Store(context);

        if (_configuredRadius is not null) {
            state.Set("Radius", _configuredRadius.Value);
        }

        return state;
    }

    protected override void BeforeCommands() {
        var placement = Registry.PlacementOf(Id);
        _reading = placement is null
            ? null
            : _sensorWorld.ReadingFor(placement, Radius);
    }

    protected override Reply? ProcessRequest(string sourceAddress, JObject request) {
        return _processor.Process(sourceAddress, request, _reading, Radius);
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
