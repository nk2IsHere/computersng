using Computers.Computer.Boundary;
using Computers.Core;
using Computers.Game.Boundary;
using StardewModdingAPI.Events;

namespace Computers.Computer;

public class ComputerTickDispatcher: ComputerEventHandler {
    
    public ComputerTickDispatcher(ContextLookup<IComputerPort> computers) : base(computers) {
    }
    
    public override ISet<Type> EventTypes => new HashSet<Type> { typeof(UpdateTickedEvent) };
    
    protected override IComputerEvent CreateEvent(IEvent @event) {
        var args = @event.Data<UpdateTickedEventArgs>();
        return new TickComputerEvent(args.Ticks);
    }
}
