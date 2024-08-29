using Computers.Computer.Boundary;
using Computers.Core;
using Computers.Game.Boundary;
using StardewModdingAPI.Events;

namespace Computers.Computer;

public class ComputerTickDispatcher: IEventHandler {
    
    private readonly ContextLookup<IComputerPort> _computers;

    public ComputerTickDispatcher(ContextLookup<IComputerPort> computers) {
        _computers = computers;
    }

    public ISet<Type> EventTypes => new HashSet<Type> { typeof(UpdateTickedEvent) };
    
    public void Handle(IEvent @event) {
        var eventArgs = @event.Data<UpdateTickedEventArgs>();
        _computers
            .Get()
            .ForEach(computer => computer.Value.Fire(new TickComputerEvent(eventArgs.Ticks)));
    }
}
