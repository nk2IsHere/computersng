using StardewModdingAPI.Events;

namespace Computers.Game.Boundary;

public record ButtonPressedEvent(
    ButtonPressedEventArgs Args
) : IEvent {
    public Type EventType => typeof(ButtonPressedEvent);
    public T Data<T>() => (T)(object)Args;
}
