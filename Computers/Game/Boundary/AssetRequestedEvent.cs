using StardewModdingAPI.Events;

namespace Computers.Game.Boundary;

public record AssetRequestedEvent(
    AssetRequestedEventArgs Args
) : IEvent {
    public Type EventType => typeof(AssetRequestedEvent);
    public T Data<T>() => (T)(object)Args;
}