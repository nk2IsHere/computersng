namespace Computers.Peripheral.Domain;

public enum MachineState {
    Empty,
    Working,
    Ready
}

public record MachinePosition(int X, int Y);

public record MachineSnapshot(
    int X,
    int Y,
    string ItemId,
    string Name,
    MachineState State,
    string? HeldItem,
    int MinutesUntilReady
);

public record ChestItemSnapshot(string Id, string Name, int Count);

public record ChestSnapshot(int X, int Y, IReadOnlyList<ChestItemSnapshot> Items);

public record GroupSnapshot(
    bool Truncated,
    IReadOnlyList<MachineSnapshot> Machines,
    IReadOnlyList<ChestSnapshot> Chests
);

public record CollectedItem(MachinePosition Machine, string Id, string Name, int Count);

public record CollectResult(IReadOnlyList<CollectedItem> Moved);

public record InsertResult(MachinePosition Machine, string Loaded);

public record PingResult(string Type);

public record MachineReadyEvent(string Event, MachineSnapshot Machine);

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
