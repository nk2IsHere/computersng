using Computers.Core;

namespace Computers.Router;

public interface IRouterEvent {
    T Data<T>();
    bool Global => true;
    bool BelongsTo(Id id) => true;
}

public record TickRouterEvent(uint Ticks) : IRouterEvent {
    public T Data<T>() => (T) (object) Ticks;
}

public record StopRouterEvent(Id? RouterId = null) : IRouterEvent {
    public T Data<T>() => default!;
    public bool Global => RouterId is null;
    public bool BelongsTo(Id id) => RouterId is null || RouterId == id;
}

public record StartRouterEvent(Id? RouterId = null) : IRouterEvent {
    public T Data<T>() => default!;
    public bool Global => RouterId is null;
    public bool BelongsTo(Id id) => RouterId is null || RouterId == id;
}
