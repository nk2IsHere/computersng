using StardewModdingAPI.Events;

namespace Computers.Game.Boundary;

public record ButtonReleasedEvent(
    ButtonReleasedEventArgs Args
) : IEvent {
    public Type EventType => typeof(ButtonReleasedEvent);
    public T Data<T>() => (T)(object)Args;
}
