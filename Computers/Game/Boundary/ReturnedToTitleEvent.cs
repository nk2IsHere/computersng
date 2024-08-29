using StardewModdingAPI.Events;

namespace Computers.Game.Boundary;

public record ReturnedToTitleEvent(
    ReturnedToTitleEventArgs Args
): IEvent {
    public Type EventType => typeof(ReturnedToTitleEvent);
    public T Data<T>() => (T)(object)Args;
}
