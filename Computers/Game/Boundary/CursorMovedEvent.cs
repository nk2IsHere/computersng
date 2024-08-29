using StardewModdingAPI.Events;

namespace Computers.Game.Boundary;

public record CursorMovedEvent(
    CursorMovedEventArgs Args
): IEvent {
    public Type EventType => typeof(CursorMovedEvent);
    public T Data<T>() => (T)(object)Args;
}
