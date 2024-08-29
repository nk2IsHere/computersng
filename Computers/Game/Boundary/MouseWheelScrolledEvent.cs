using StardewModdingAPI.Events;

namespace Computers.Game.Boundary;

public record MouseWheelScrolledEvent(
    MouseWheelScrolledEventArgs Args
): IEvent {
    public Type EventType => typeof(MouseWheelScrolledEvent);
    public T Data<T>() => (T)(object)Args;
}
