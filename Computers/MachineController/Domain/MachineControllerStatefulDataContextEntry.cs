using System.Collections.Concurrent;
using Computers.Computer;
using Computers.Core;
using Computers.MachineController.Domain.Wire;
using Computers.Peripheral;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Router.Domain.Wire;
using StardewModdingAPI;
using Context = Computers.Core.Context;

namespace Computers.MachineController.Domain;

public class MachineControllerStatefulDataContextEntry :
    IContextEntry.StatefulDataContextEntry<MachineControllerStatefulDataContextEntry>, IPeripheralPort {

    private readonly IMonitor _monitor;
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<IRouterPort> _routers;
    private readonly IMachineWorld _machineWorld;

    private readonly ConcurrentQueue<Datagram> _inbox = new();
    private readonly DelegatingOps _ops = new();
    private readonly MachineControllerCommandProcessor _processor;

    private MachineGroup? _cachedGroup;
    private volatile bool _groupDirty = true;
    private HashSet<(int X, int Y)> _previousReady = new();

    private volatile bool _isEnabled;

    public MachineControllerStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        IMachineWorld machineWorld
    ) : base(factoryId, id) {
        _monitor = monitor;
        Configuration = configuration;
        _registry = registry;
        _routers = routers;
        _machineWorld = machineWorld;
        _processor = new MachineControllerCommandProcessor(_ops, configuration.MachineController.MaxSubscribersPerPeripheral);
    }

    private class DelegatingOps : IMachineGroupOps {
        public IMachineGroupOps? Inner { get; set; }

        private IMachineGroupOps Require() =>
            Inner ?? throw new GroupOpException("controller has no world position");

        public GroupSnapshot ListSnapshot() => Require().ListSnapshot();
        public CollectResult Collect(MachinePosition? target) => Require().Collect(target);
        public InsertResult Insert(InsertRequest request) => Require().Insert(request);
        public void Rescan() => Require().Rescan();
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
        _monitor.Log($"Machine controller {Id} started");
        _isEnabled = true;
    }

    public void Stop() {
        _monitor.Log($"Machine controller {Id} stopped");
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
            _monitor.Log($"Machine controller {Id}: inbox full, dropping datagram {datagram.MessageId}");
            return;
        }

        _inbox.Enqueue(datagram);
    }

    public void NotifyWorldChanged() {
        _groupDirty = true;
    }

    private void Tick() {
        if (!_isEnabled) {
            return;
        }

        var placement = _registry.PlacementOf(Id);
        var location = placement is null ? null : _machineWorld.LocationOf(placement);
        if (placement is null || location is null) {
            _ops.Inner = null;
        }
        else {
            var group = EnsureGroup(location, placement);
            _ops.Inner = location.Ops(group, NotifyWorldChanged);
        }

        DrainCommands();
        PushReadyTransitions(location);
    }

    private MachineGroup EnsureGroup(IMachineLocation location, Placement placement) {
        if (!_groupDirty && _cachedGroup is not null) {
            return _cachedGroup;
        }

        _cachedGroup = MachineGroupScanner.Scan(
            location.GroupWorld,
            placement.X,
            placement.Y,
            Configuration.MachineController.MaxGroupSize
        );
        _groupDirty = false;

        if (_cachedGroup.Truncated) {
            _monitor.Log(
                $"Machine controller {Id}: group truncated at {Configuration.MachineController.MaxGroupSize} objects (machineController.maxGroupSize).",
                LogLevel.Warn
            );
        }

        return _cachedGroup;
    }

    private void DrainCommands() {
        var processed = 0;
        while (processed < Configuration.MachineController.MaxCommandsPerTick && _inbox.TryDequeue(out var datagram)) {
            processed++;

            // Transport edge: strings and JSON parsing live here; the processor is typed.
            Newtonsoft.Json.Linq.JObject request;
            try {
                request = Newtonsoft.Json.Linq.JObject.Parse(datagram.Payload);
            } catch (Newtonsoft.Json.JsonReaderException) {
                _monitor.Log($"Machine controller {Id}: dropped undecodable datagram from {datagram.SourceAddress}.");
                continue;
            }

            var reply = _processor.Process(datagram.SourceAddress, request);
            if (reply is null) {
                _monitor.Log($"Machine controller {Id}: dropped datagram without correlation id from {datagram.SourceAddress}.");
                continue;
            }

            SendTo(datagram.SourceAddress, WireJson.Serialize(reply));
        }
    }

    private void PushReadyTransitions(IMachineLocation? location) {
        var currentReady = location is not null && _cachedGroup is not null
            ? new HashSet<(int X, int Y)>(location.ReadyMachines(_cachedGroup.Machines))
            : new HashSet<(int X, int Y)>();

        if (_processor.ReadySubscribers.Count > 0 && location is not null) {
            foreach (var (x, y) in currentReady.Except(_previousReady)) {
                if (location.Snapshot(x, y) is not { } snapshot) {
                    continue;
                }

                var push = WireJson.Serialize(MachineControllerCommandProcessor.BuildMachineReadyEvent(snapshot));

                foreach (var subscriber in _processor.ReadySubscribers) {
                    SendTo(subscriber, push);
                }
            }
        }

        _previousReady = currentReady;
    }

    private void SendTo(string targetAddress, string payload) {
        var sent = NetworkDispatch.Send(
            _registry,
            _routers,
            Id,
            targetAddress,
            payload,
            Configuration.Network.MessageTtl
        );

        if (!sent) {
            _monitor.Log($"Machine controller {Id}: offline (no covering router), dropped message to {targetAddress}.");
        }
    }

}
