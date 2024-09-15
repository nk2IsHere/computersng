using Computers.Core;
using Computers.Game;
using StardewModdingAPI.Events;

namespace Computers.Router.Domain.Event;

public class RouterTickDispatcher : RouterEventHandler {
    
    public RouterTickDispatcher(ContextLookup<IRouterPort> routers) : base(routers) {
    }

    public override ISet<Type> EventTypes => new HashSet<Type> { typeof(UpdateTickedEvent) };
    
    protected override IRouterEvent CreateEvent(IEvent @event) {
        var args = @event.Data<UpdateTickedEventArgs>();
        return new TickRouterEvent(args.Ticks);
    }
}
