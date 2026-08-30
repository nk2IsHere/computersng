using Computers.Peripheral.Domain;
using Xunit;

namespace Computers.Tests.Peripheral;

public class MachineGroupScannerTests {
    private class GridWorld : IGroupWorld {
        private readonly Dictionary<(int X, int Y), GroupCellKind> _cells;

        public GridWorld(Dictionary<(int, int), GroupCellKind> cells) {
            _cells = cells;
        }

        public GroupCellKind KindAt(int x, int y) =>
            _cells.GetValueOrDefault((x, y), GroupCellKind.Empty);
    }

    [Fact]
    public void AdjacentMachineJoinsTheGroup() {
        var world = new GridWorld(new() {
            [(1, 0)] = GroupCellKind.Machine
        });

        var group = MachineGroupScanner.Scan(world, 0, 0, 256);

        Assert.Equal(new[] { (1, 0) }, group.Machines);
        Assert.Empty(group.Chests);
        Assert.False(group.Truncated);
    }

    [Fact]
    public void ChainsExtendThroughMachinesAndChests() {
        // controller(0,0) - machine(1,0) - machine(2,0) - chest(2,1)
        var world = new GridWorld(new() {
            [(1, 0)] = GroupCellKind.Machine,
            [(2, 0)] = GroupCellKind.Machine,
            [(2, 1)] = GroupCellKind.Chest
        });

        var group = MachineGroupScanner.Scan(world, 0, 0, 256);

        Assert.Equal(2, group.Machines.Count);
        Assert.Equal(new[] { (2, 1) }, group.Chests);
    }

    [Fact]
    public void DiagonalNeighborsAreExcluded() {
        var world = new GridWorld(new() {
            [(1, 1)] = GroupCellKind.Machine // diagonal only
        });

        var group = MachineGroupScanner.Scan(world, 0, 0, 256);

        Assert.Empty(group.Machines);
    }

    [Fact]
    public void ConnectorsBridgeSeparateClusters() {
        // controller(0,0) - connector(1,0) - machine(2,0)
        var world = new GridWorld(new() {
            [(1, 0)] = GroupCellKind.Connector,
            [(2, 0)] = GroupCellKind.Machine
        });

        var group = MachineGroupScanner.Scan(world, 0, 0, 256);

        Assert.Equal(new[] { (2, 0) }, group.Machines);
    }

    [Fact]
    public void CapTruncatesTheFill() {
        var cells = new Dictionary<(int, int), GroupCellKind>();
        for (var x = 1; x <= 10; x++) {
            cells[(x, 0)] = GroupCellKind.Machine;
        }
        var world = new GridWorld(cells);

        var group = MachineGroupScanner.Scan(world, 0, 0, maxGroupSize: 5);

        Assert.True(group.Truncated);
        Assert.Equal(5, group.Machines.Count);
    }

    [Fact]
    public void EmptySurroundingsYieldEmptyGroup() {
        var group = MachineGroupScanner.Scan(new GridWorld(new()), 0, 0, 256);

        Assert.Empty(group.Machines);
        Assert.Empty(group.Chests);
        Assert.False(group.Truncated);
    }
}
