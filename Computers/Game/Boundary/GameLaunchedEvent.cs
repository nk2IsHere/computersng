using StardewModdingAPI.Events;

namespace Computers.Game.Boundary;

public record GameLaunchedEvent(
    GameLaunchedEventArgs Args
) : IEvent {
    public Type EventType => typeof(GameLaunchedEvent);
    public T Data<T>() => (T)(object)Args;
}