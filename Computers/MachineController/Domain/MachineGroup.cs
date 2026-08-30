namespace Computers.MachineController.Domain;

public enum GroupCellKind {
    Empty,
    Machine,
    Chest,
    Connector
}

public interface IGroupWorld {
    GroupCellKind KindAt(int x, int y);
}

public record GroupMember(int X, int Y, GroupCellKind Kind);

public record MachineGroup(IReadOnlyList<GroupMember> Members, bool Truncated) {
    public IEnumerable<(int X, int Y)> Machines => Positions(GroupCellKind.Machine);

    public IEnumerable<(int X, int Y)> Chests => Positions(GroupCellKind.Chest);

    public bool Contains(GroupCellKind kind, int x, int y) {
        return Members.Any(member => member.Kind == kind && member.X == x && member.Y == y);
    }

    private IEnumerable<(int X, int Y)> Positions(GroupCellKind kind) {
        return Members.Where(member => member.Kind == kind).Select(member => (member.X, member.Y));
    }
}

public static class MachineGroupScanner {
    private static readonly (int Dx, int Dy)[] Edges = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    public static MachineGroup Scan(IGroupWorld world, int originX, int originY, int maxGroupSize) {
        var members = new List<GroupMember>();
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

                members.Add(new GroupMember(next.X, next.Y, kind));
                frontier.Enqueue(next);
            }
        }

        return new MachineGroup(members, truncated);
    }
}
