using Computers.Core;
using Computers.Game;

namespace Computers.Router.Domain.Event;

public class RouterStopDispatcher : RouterEventHandler {
    
    public RouterStopDispatcher(ContextLookup<IRouterPort> routers) : base(routers) {
    }

    public override ISet<Type> EventTypes => new HashSet<Type> { typeof(StopRouterEvent) };
    
    protected override IRouterEvent CreateEvent(IEvent @event) {
        return new StopRouterEvent();
    }
}
