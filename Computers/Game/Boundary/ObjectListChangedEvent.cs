using StardewModdingAPI.Events;

namespace Computers.Game.Boundary;

public record ObjectListChangedEvent(
    ObjectListChangedEventArgs Args
): IEvent {
    public Type EventType => typeof(ObjectListChangedEvent);
    public T Data<T>() => (T)(object)Args;
}
