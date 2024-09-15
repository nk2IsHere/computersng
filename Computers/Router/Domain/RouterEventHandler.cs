using Computers.Core;
using Computers.Game;

namespace Computers.Router.Domain;

public abstract class RouterEventHandler: IEventHandler {
    
    private readonly ContextLookup<IRouterPort> _routers;

    protected RouterEventHandler(ContextLookup<IRouterPort> routers) {
        _routers = routers;
    }
    
    public abstract ISet<Type> EventTypes { get; }
    
    public void Handle(IEvent @event) {
        var routerEvent = CreateEvent(@event);
        _routers
            .Get()
            .Where(router => routerEvent.BelongsTo(router.Id))
            .ForEach(router => router.Value.Fire(routerEvent));
    }
    
    protected abstract IRouterEvent CreateEvent(IEvent @event);
}
