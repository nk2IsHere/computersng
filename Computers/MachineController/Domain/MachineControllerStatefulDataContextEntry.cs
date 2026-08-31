using Computers.Computer;
using Computers.Core;
using Computers.MachineController.Domain.Wire;
using Computers.Peripheral;
using Computers.Peripheral.Domain;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace Computers.MachineController.Domain;

public class MachineControllerStatefulDataContextEntry : PeripheralEntity<MachineControllerStatefulDataContextEntry> {

    private readonly IMachineWorld _machineWorld;
    private readonly PeripheralTier _tier;
    private readonly DelegatingOps _ops = new();
    private readonly PeripheralSubscriptions _subscriptions;
    private readonly MachineControllerCommandProcessor _processor;

    private MachineGroup? _cachedGroup;
    private volatile bool _groupDirty = true;
    private HashSet<(int X, int Y)> _previousReady = new();
    private IMachineLocation? _location;

    public MachineControllerStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        IMachineWorld machineWorld,
        PeripheralTier tier
    ) : base(factoryId, id, monitor, configuration, registry, routers) {
        _machineWorld = machineWorld;
        _tier = tier;
        _subscriptions = new PeripheralSubscriptions(new[] { "ready" }, configuration.Peripheral.MaxSubscribers);
        _processor = new MachineControllerCommandProcessor(_ops, _subscriptions, tier);
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

    public override void NotifyWorldChanged() {
        _groupDirty = true;
    }

    protected override void BeforeCommands() {
        var placement = Registry.PlacementOf(Id);
        _location = placement is null ? null : _machineWorld.LocationOf(placement);
        if (placement is null || _location is null) {
            _ops.Inner = null;
        }
        else {
            var group = EnsureGroup(_location, placement);
            _ops.Inner = _location.Ops(group, NotifyWorldChanged);
        }
    }

    protected override Reply? ProcessRequest(string sourceAddress, JObject request) {
        return _processor.Process(sourceAddress, request);
    }

    protected override void AfterCommands() {
        PushReadyTransitions(_location);
    }

    private MachineGroup EnsureGroup(IMachineLocation location, Placement placement) {
        if (!_groupDirty && _cachedGroup is not null) {
            return _cachedGroup;
        }

        _cachedGroup = MachineGroupScanner.Scan(
            location.GroupWorld,
            placement.X,
            placement.Y,
            Configuration.MachineController.MaxGroupSize,
            _tier == PeripheralTier.Basic ? 1 : int.MaxValue
        );
        _groupDirty = false;

        if (_cachedGroup.Truncated) {
            Monitor.Log(
                $"Machine controller {Id}: group truncated at {Configuration.MachineController.MaxGroupSize} objects (machineController.maxGroupSize).",
                LogLevel.Warn
            );
        }

        return _cachedGroup;
    }

    private void PushReadyTransitions(IMachineLocation? location) {
        var currentReady = location is not null && _cachedGroup is not null
            ? new HashSet<(int X, int Y)>(location.ReadyMachines(_cachedGroup.Machines))
            : new HashSet<(int X, int Y)>();

        var subscribers = _subscriptions.Subscribers("ready");
        if (subscribers.Count > 0 && location is not null) {
            foreach (var (x, y) in currentReady.Except(_previousReady)) {
                if (location.Snapshot(x, y) is not { } snapshot) {
                    continue;
                }

                var push = WireJson.Serialize(MachineControllerCommandProcessor.BuildMachineReadyEvent(snapshot));

                foreach (var subscriber in subscribers) {
                    SendTo(subscriber, push);
                }
            }
        }

        _previousReady = currentReady;
    }
}
