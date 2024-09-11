using StardewModdingAPI.Events;

namespace Computers.Game;

public record AssetRequestedEvent(
    AssetRequestedEventArgs Args
) : IEvent {
    public Type EventType => typeof(AssetRequestedEvent);
    public T Data<T>() => (T)(object)Args;
}

public record ButtonPressedEvent(
    ButtonPressedEventArgs Args
) : IEvent {
    public Type EventType => typeof(ButtonPressedEvent);
    public T Data<T>() => (T)(object)Args;
}

public record ButtonReleasedEvent(
    ButtonReleasedEventArgs Args
) : IEvent {
    public Type EventType => typeof(ButtonReleasedEvent);
    public T Data<T>() => (T)(object)Args;
}

public record GameLaunchedEvent(
    GameLaunchedEventArgs Args
) : IEvent {
    public Type EventType => typeof(GameLaunchedEvent);
    public T Data<T>() => (T)(object)Args;
}

public record ObjectListChangedEvent(
    ObjectListChangedEventArgs Args
): IEvent {
    public Type EventType => typeof(ObjectListChangedEvent);
    public T Data<T>() => (T)(object)Args;
}

public record ReturnedToTitleEvent(
    ReturnedToTitleEventArgs Args
): IEvent {
    public Type EventType => typeof(ReturnedToTitleEvent);
    public T Data<T>() => (T)(object)Args;
}

public record SaveLoadedEvent(
    SaveLoadedEventArgs Args
) : IEvent {
    public Type EventType => typeof(SaveLoadedEvent);
    public T Data<T>() => (T)(object)Args;
}

public record UpdateTickedEvent(
    UpdateTickedEventArgs Args
) : IEvent {
    public Type EventType => typeof(UpdateTickedEvent);
    public T Data<T>() => (T)(object)Args;
}
