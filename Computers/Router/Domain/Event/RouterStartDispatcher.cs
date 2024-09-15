using Computers.Core;
using Computers.Game;

namespace Computers.Router.Domain.Event;

public class RouterStartDispatcher : RouterEventHandler {
    
    public RouterStartDispatcher(ContextLookup<IRouterPort> routers) : base(routers) {
    }

    public override ISet<Type> EventTypes => new HashSet<Type> { typeof(StartRouterEvent) };

    protected override IRouterEvent CreateEvent(IEvent @event) {
        return new StartRouterEvent();
    }
}