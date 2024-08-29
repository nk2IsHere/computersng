using StardewModdingAPI.Events;

namespace Computers.Game.Boundary;

public record UpdateTickedEvent(
    UpdateTickedEventArgs Args
) : IEvent {
    public Type EventType => typeof(UpdateTickedEvent);
    public T Data<T>() => (T)(object)Args;
}
