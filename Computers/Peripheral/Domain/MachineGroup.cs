namespace Computers.Peripheral.Domain;

public enum GroupCellKind {
    Empty,
    Machine,
    Chest,
    Connector
}

public interface IGroupWorld {
    GroupCellKind KindAt(int x, int y);
}

public record MachineGroup(
    IReadOnlyList<(int X, int Y)> Machines,
    IReadOnlyList<(int X, int Y)> Chests,
    bool Truncated
);

public static class MachineGroupScanner {
    private static readonly (int Dx, int Dy)[] Edges = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    public static MachineGroup Scan(IGroupWorld world, int originX, int originY, int maxGroupSize) {
        var machines = new List<(int X, int Y)>();
        var chests = new List<(int X, int Y)>();
        var visited = new HashSet<(int X, int Y)> { (originX, originY) };
        var frontier = new Queue<(int X, int Y)>();
        frontier.Enqueue((originX, originY));

        var visitedCells = 0;
        var truncated = false;

        while (frontier.Count > 0) {
            var (x, y) = frontier.Dequeue();

            foreach (var (dx, dy) in Edges) {
                var next = (X: x + dx, Y: y + dy);
                if (!visited.Add(next)) {
                    continue;
                }

                var kind = world.KindAt(next.X, next.Y);
                if (kind == GroupCellKind.Empty) {
                    continue;
                }

                if (visitedCells >= maxGroupSize) {
                    truncated = true;
                    continue;
                }
                visitedCells++;

                switch (kind) {
                    case GroupCellKind.Machine:
                        machines.Add(next);
                        break;
                    case GroupCellKind.Chest:
                        chests.Add(next);
                        break;
                }

                frontier.Enqueue(next);
            }
        }

        return new MachineGroup(machines, chests, truncated);
    }
}
