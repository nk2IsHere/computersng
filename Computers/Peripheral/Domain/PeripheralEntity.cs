using System.Collections.Concurrent;
using Computers.Computer;
using Computers.Core;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using Context = Computers.Core.Context;

namespace Computers.Peripheral.Domain;

// Wire-visible request failure shared by peripheral firmware. Domain specific exceptions
// derive from it so processors handle one type.
public class PeripheralRequestException : Exception {
    public PeripheralRequestException(string error) : base(error) {
    }
}

// Every well-behaved peripheral answers ping with this self-description.
public record PingResult(string Type);

// Base for peripheral entities. Owns the inbox with its queue limit, the per tick command
// drain with the JSON transport edge, replies through NetworkDispatch and the lifecycle.
public abstract class PeripheralEntity<T> : IContextEntry.StatefulDataContextEntry<T>, IPeripheralPort
    where T : PeripheralEntity<T> {

    protected readonly IMonitor Monitor;
    protected readonly NetworkRegistry Registry;
    private readonly ContextLookup<IRouterPort> _routers;

    private readonly ConcurrentQueue<Datagram> _inbox = new();
    private volatile bool _isEnabled;

    protected PeripheralEntity(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers
    ) : base(factoryId, id) {
        Monitor = monitor;
        Configuration = configuration;
        Registry = registry;
        _routers = routers;
    }

    public Configuration Configuration { get; }

    public bool IsEnabled => _isEnabled;

    internal int PendingCommands => _inbox.Count;

    public override object GetValue(Context context) {
        return this;
    }

    public override void Restore(Context context, ContextEntryState state) {
    }

    public override ContextEntryState Store(Context context) {
        var state = ContextEntryState.Empty;

        state.Id = Id;
        state.FactoryId = FactoryId;

        return state;
    }

    public void Start() {
        Monitor.Log($"Peripheral {Id} started");
        _isEnabled = true;
    }

    public void Stop() {
        Monitor.Log($"Peripheral {Id} stopped");
        _isEnabled = false;
        _inbox.Clear();
    }

    public void Fire(IPeripheralEvent peripheralEvent) {
        if (peripheralEvent is TickPeripheralEvent) {
            Tick();
        }

        if (peripheralEvent is StopPeripheralEvent) {
            Stop();
        }

        if (peripheralEvent is StartPeripheralEvent) {
            Start();
            NotifyWorldChanged();
        }
    }

    public void ReceiveDatagram(Datagram datagram) {
        if (!_isEnabled) {
            return;
        }

        if (_inbox.Count >= Configuration.Network.RouterQueueLimit) {
            Monitor.Log($"Peripheral {Id}: inbox full, dropping datagram {datagram.MessageId}");
            return;
        }

        _inbox.Enqueue(datagram);
    }

    public virtual void NotifyWorldChanged() {
    }

    // Runs before the queued commands each tick, typically to refresh world-derived state.
    protected virtual void BeforeCommands() {
    }

    // Runs after the queued commands each tick, typically to push subscribed events.
    protected virtual void AfterCommands() {
    }

    // Answers one request. Null means the payload had no correlation id and is dropped.
    protected abstract Reply? ProcessRequest(string sourceAddress, JObject request);

    private void Tick() {
        if (!_isEnabled) {
            return;
        }

        BeforeCommands();
        DrainCommands();
        AfterCommands();
    }

    private void DrainCommands() {
        var processed = 0;
        while (processed < Configuration.Peripheral.MaxCommandsPerTick && _inbox.TryDequeue(out var datagram)) {
            processed++;

            // Transport edge. Strings and JSON parsing live here and the processors are typed.
            JObject request;
            try {
                request = JObject.Parse(datagram.Payload);
            } catch (JsonReaderException) {
                Monitor.Log($"Peripheral {Id}: dropped undecodable datagram from {datagram.SourceAddress}.");
                continue;
            }

            var reply = ProcessRequest(datagram.SourceAddress, request);
            if (reply is null) {
                Monitor.Log($"Peripheral {Id}: dropped datagram without correlation id from {datagram.SourceAddress}.");
                continue;
            }

            SendTo(datagram.SourceAddress, WireJson.Serialize(reply));
        }
    }

    protected void SendTo(string targetAddress, string payload) {
        var sent = NetworkDispatch.Send(
            Registry,
            _routers,
            Id,
            targetAddress,
            payload,
            Configuration.Network.MessageTtl
        );

        if (!sent) {
            Monitor.Log($"Peripheral {Id}: offline (no covering router), dropped message to {targetAddress}.");
        }
    }
}
