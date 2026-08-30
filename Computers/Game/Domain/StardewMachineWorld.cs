using Computers.MachineController;
using Computers.Peripheral;
using Computers.MachineController.Domain;
using Computers.MachineController.Domain.Wire;
using Computers.Router.Domain;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace Computers.Game.Domain;

public class StardewMachineWorld : IMachineWorld {
    public IMachineLocation? LocationOf(Placement placement) {
        var location = Game1.getLocationFromName(placement.Location);
        return location is null ? null : new StardewMachineLocation(location);
    }
}

internal class StardewMachineLocation : IMachineLocation {
    private readonly GameLocation _location;

    public StardewMachineLocation(GameLocation location) {
        _location = location;
    }

    public IGroupWorld GroupWorld => new StardewGroupWorld(_location);

    public IMachineGroupOps Ops(MachineGroup group, Action invalidateGroup) {
        return new StardewMachineGroupOps(_location, group, invalidateGroup);
    }

    public IReadOnlySet<(int X, int Y)> ReadyMachines(IEnumerable<(int X, int Y)> machines) {
        var ready = new HashSet<(int X, int Y)>();
        foreach (var (x, y) in machines) {
            if (_location.objects.TryGetValue(new Vector2(x, y), out var machine) && machine.readyForHarvest.Value) {
                ready.Add((x, y));
            }
        }

        return ready;
    }

    public MachineMemberSnapshot? Snapshot(int x, int y) {
        return _location.objects.TryGetValue(new Vector2(x, y), out var machine)
            ? StardewMachineGroupOps.SnapshotMachine(x, y, machine)
            : null;
    }
}

internal class StardewGroupWorld : IGroupWorld {
    private readonly GameLocation _location;

    public StardewGroupWorld(GameLocation location) {
        _location = location;
    }

    public GroupCellKind KindAt(int x, int y) {
        if (!_location.objects.TryGetValue(new Vector2(x, y), out var obj)) {
            return GroupCellKind.Empty;
        }

        if (obj is Chest) {
            return GroupCellKind.Chest;
        }

        if (obj.modData.ContainsKey("PeripheralId")) {
            return GroupCellKind.Connector;
        }

        return obj.GetMachineData() != null ? GroupCellKind.Machine : GroupCellKind.Empty;
    }
}

internal class StardewMachineGroupOps : IMachineGroupOps {
    private readonly GameLocation _location;
    private readonly MachineGroup _group;
    private readonly Action _invalidateGroup;

    public StardewMachineGroupOps(GameLocation location, MachineGroup group, Action invalidateGroup) {
        _location = location;
        _group = group;
        _invalidateGroup = invalidateGroup;
    }

    public GroupSnapshot ListSnapshot() {
        var members = new List<GroupMemberSnapshot>();
        foreach (var member in _group.Members) {
            switch (member.Kind) {
                case GroupCellKind.Machine when ObjectAt(member.X, member.Y) is { } machine:
                    members.Add(SnapshotMachine(member.X, member.Y, machine));
                    break;

                case GroupCellKind.Chest when ObjectAt(member.X, member.Y) is Chest chest:
                    members.Add(new ChestMemberSnapshot(
                        member.X,
                        member.Y,
                        chest.Items
                            .Where(item => item is not null)
                            .Select(item => new ChestItemSnapshot(item.ItemId, item.Name, item.Stack))
                            .ToList()
                    ));
                    break;

                case GroupCellKind.Connector:
                    members.Add(new ConnectorMemberSnapshot(member.X, member.Y));
                    break;
            }
        }

        return new GroupSnapshot(_group.Truncated, members);
    }

    public CollectResult Collect(MachinePosition? target) {
        var targets = target is null
            ? _group.Machines.ToList()
            : new List<(int X, int Y)> { (target.X, target.Y) };

        if (target is not null && !_group.Contains(GroupCellKind.Machine, target.X, target.Y)) {
            throw new GroupOpException("machine not in group");
        }

        var moved = new List<CollectedItem>();
        foreach (var (machineX, machineY) in targets) {
            var machine = ObjectAt(machineX, machineY);
            if (machine is null || !machine.readyForHarvest.Value) {
                if (targets.Count == 1) {
                    throw new GroupOpException("machine is not ready");
                }
                continue;
            }

            var output = machine.heldObject.Value;
            if (output is null) {
                continue;
            }

            if (!TryStoreInGroupChests(output)) {
                if (targets.Count == 1) {
                    throw new GroupOpException("no chest space for the output");
                }
                continue;
            }

            moved.Add(new CollectedItem(
                new MachinePosition(machineX, machineY),
                output.ItemId,
                output.Name,
                output.Stack
            ));

            // Vanilla harvest reset (CheckForActionOnMachine), minus player inventory/stats/XP.
            var machineData = machine.GetMachineData();
            machine.heldObject.Value = null;
            machine.readyForHarvest.Value = false;
            machine.showNextIndex.Value = false;
            machine.ResetParentSheetIndex();

            if (MachineDataUtility.TryGetMachineOutputRule(
                    machine, machineData, MachineOutputTrigger.OutputCollected,
                    output.getOne(), Game1.player, _location,
                    out var rule, out _, out _, out _)) {
                machine.OutputMachine(machineData, rule, machine.lastInputItem.Value, Game1.player, _location, probe: false);
            }
        }

        if (moved.Count == 0 && targets.Count > 1) {
            throw new GroupOpException("nothing ready to collect");
        }

        return new CollectResult(moved);
    }

    public InsertResult Insert(InsertRequest request) {
        if (request.Count != 1) {
            throw new GroupOpException("count > 1 is not supported: a machine holds one job; repeat insert instead");
        }

        var (x, y) = (request.Machine.X, request.Machine.Y);
        if (!_group.Contains(GroupCellKind.Machine, x, y)) {
            throw new GroupOpException("machine not in group");
        }

        var machine = ObjectAt(x, y);
        if (machine is null) {
            throw new GroupOpException("machine not found in the world");
        }

        if (machine.heldObject.Value is not null) {
            throw new GroupOpException("machine is already occupied");
        }

        var chest = FindSourceChest(request.ItemId, request.FromChest, out var item);

        // Vanilla auto-load
        Object.autoLoadFrom = chest.Items;
        bool loaded;
        try {
            loaded = machine.performObjectDropInAction(item, probe: false, Game1.player);
        } finally {
            Object.autoLoadFrom = null;
        }

        if (!loaded) {
            throw new GroupOpException("machine did not accept the item (missing ingredients or wrong input?)");
        }

        return new InsertResult(request.Machine, item.Name);
    }

    public void Rescan() {
        _invalidateGroup();
    }

    public static MachineMemberSnapshot SnapshotMachine(int x, int y, Object machine) {
        var state = machine.readyForHarvest.Value
            ? MachineState.Ready
            : machine.heldObject.Value is not null || machine.MinutesUntilReady > 0
                ? MachineState.Working
                : MachineState.Empty;

        return new MachineMemberSnapshot(
            x,
            y,
            machine.ItemId,
            machine.Name,
            state,
            machine.heldObject.Value?.Name,
            machine.MinutesUntilReady
        );
    }

    private Object? ObjectAt(int x, int y) {
        return _location.objects.TryGetValue(new Vector2(x, y), out var obj) ? obj : null;
    }

    private bool TryStoreInGroupChests(Object output) {
        foreach (var (chestX, chestY) in _group.Chests) {
            if (ObjectAt(chestX, chestY) is not Chest chest) {
                continue;
            }

            var leftover = chest.addItem(output);
            if (leftover is null) {
                return true;
            }
        }

        return false;
    }

    private Chest FindSourceChest(string itemId, MachinePosition? fromChest, out Item item) {
        var candidates = fromChest is not null
            ? new List<(int X, int Y)> { (fromChest.X, fromChest.Y) }
            : _group.Chests.ToList();

        if (fromChest is not null && !_group.Contains(GroupCellKind.Chest, fromChest.X, fromChest.Y)) {
            throw new GroupOpException("chest not in group");
        }

        foreach (var (x, y) in candidates) {
            if (ObjectAt(x, y) is not Chest chest) {
                continue;
            }

            var found = chest.Items.FirstOrDefault(
                candidate => candidate is not null
                    && (candidate.ItemId == itemId || candidate.QualifiedItemId == itemId));
            if (found is not null) {
                item = found;
                return chest;
            }
        }

        throw new GroupOpException($"item '{itemId}' not found in group chests");
    }
}
