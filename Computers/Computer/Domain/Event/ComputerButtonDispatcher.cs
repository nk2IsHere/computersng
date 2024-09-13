using Computers.Core;
using Computers.Game;
using StardewModdingAPI.Events;
using ButtonReleasedEvent = Computers.Game.ButtonReleasedEvent;

namespace Computers.Computer.Domain.Event;

public class ComputerButtonDispatcher: ComputerEventHandler {
    public ComputerButtonDispatcher(ContextLookup<IComputerPort> computers) : base(computers) {
    }

    public override ISet<Type> EventTypes => new HashSet<Type> {
        typeof(ButtonPressedEvent),
        typeof(ButtonReleasedEvent)
    };
    
    protected override IComputerEvent CreateEvent(IEvent @event) {
        if(@event.EventType == typeof(ButtonPressedEvent)) {
            var args = @event.Data<ButtonPressedEventArgs>();
            return new ButtonHeldEvent(args.Button);
        }
        
        if(@event.EventType == typeof(ButtonReleasedEvent)) {
            var args = @event.Data<ButtonReleasedEventArgs>();
            return new ButtonUnheldEvent(args.Button);
        }
        
        throw new ArgumentException("Event type not supported");
    }
}
