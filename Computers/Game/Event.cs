using Computers.Core;
using StardewModdingAPI;

namespace Computers.Game;

public interface IEvent {
    Type EventType { get; }
    T Data<T>();
}

public interface IEventHandler {
    ISet<Type> EventTypes { get; }
    void Handle(IEvent @event);
}

public interface IEventBus {
    void Publish(IEvent @event);
}

public class EventBus : IEventBus {
    private readonly IMonitor _monitor;
    private readonly ContextLookup<IEventHandler> _handlers;

    public EventBus(
        IMonitor monitor,
        ContextLookup<IEventHandler> handlers
    ) {
        _monitor = monitor;
        _handlers = handlers;
    }

    public void Publish(IEvent @event) {
        var eventType = @event.EventType;
        _handlers
            .Get()
            .Where(handler => handler.Value.EventTypes.Contains(eventType))
            .ForEach(handler => {
                handler.Value.Handle(@event);
            });
    }
}
