using Computers.Computer.Boundary;
using Computers.Core;
using Computers.Game.Boundary;

namespace Computers.Computer;

public abstract class ComputerEventHandler: IEventHandler {
    
    private readonly ContextLookup<IComputerPort> _computers;

    public ComputerEventHandler(ContextLookup<IComputerPort> computers) {
        _computers = computers;
    }
    
    public abstract ISet<Type> EventTypes { get; }
    
    public void Handle(IEvent @event) {
        var computerEvent = CreateEvent(@event);
        _computers
            .Get()
            .Where(computer => computerEvent.BelongsTo(computer.Id))
            .ForEach(computer => computer.Value.Fire(computerEvent));
    }
    
    protected abstract IComputerEvent CreateEvent(IEvent @event);
}
