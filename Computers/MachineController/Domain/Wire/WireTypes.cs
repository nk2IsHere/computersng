namespace Computers.MachineController.Domain.Wire;

public enum MachineState {
    Empty,
    Working,
    Ready
}

public record MachinePosition(int X, int Y);

public abstract record GroupMemberSnapshot(int X, int Y);

public record MachineMemberSnapshot(
    int X,
    int Y,
    string ItemId,
    string Name,
    MachineState State,
    string? HeldItem,
    int MinutesUntilReady
) : GroupMemberSnapshot(X, Y) {
    public string Kind => "machine";
}

public record ChestItemSnapshot(string Id, string Name, int Count);

public record ChestMemberSnapshot(
    int X,
    int Y,
    IReadOnlyList<ChestItemSnapshot> Items
) : GroupMemberSnapshot(X, Y) {
    public string Kind => "chest";
}

public record ConnectorMemberSnapshot(int X, int Y) : GroupMemberSnapshot(X, Y) {
    public string Kind => "connector";
}

public record GroupSnapshot(bool Truncated, IReadOnlyList<GroupMemberSnapshot> Members);

public record CollectedItem(MachinePosition Machine, string Id, string Name, int Count);

public record CollectResult(IReadOnlyList<CollectedItem> Moved);

public record InsertResult(MachinePosition Machine, string Loaded);

public record PingResult(string Type);

public record MachineReadyEvent(string Event, MachineMemberSnapshot Machine);

public abstract record PeripheralRequest;

public record PingRequest : PeripheralRequest;

public record ListRequest : PeripheralRequest;

public record RescanRequest : PeripheralRequest;

public record CollectRequest(MachinePosition? Machine) : PeripheralRequest;

public record InsertRequest(
    MachinePosition Machine,
    string ItemId,
    int Count,
    MachinePosition? FromChest
) : PeripheralRequest;

public record SubscribeRequest(IReadOnlyList<string> Events) : PeripheralRequest;

public record UnsubscribeRequest(IReadOnlyList<string> Events) : PeripheralRequest;
