using Computers.Core;

namespace Computers.Peripheral;

public interface IPeripheralEvent {
    bool Global => true;
    bool BelongsTo(Id id) => true;
}

public record TickPeripheralEvent(uint Ticks) : IPeripheralEvent;

public record StartPeripheralEvent(Id? PeripheralId = null) : IPeripheralEvent {
    public bool Global => PeripheralId is null;
    public bool BelongsTo(Id id) => PeripheralId is null || PeripheralId == id;
}

public record StopPeripheralEvent(Id? PeripheralId = null) : IPeripheralEvent {
    public bool Global => PeripheralId is null;
    public bool BelongsTo(Id id) => PeripheralId is null || PeripheralId == id;
}
