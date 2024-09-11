using Computers.Core;
using Computers.Game;

namespace Computers.Computer.Domain;

public class ComputerStopDispatcher: ComputerEventHandler {
    
    public ComputerStopDispatcher(ContextLookup<IComputerPort> computers) : base(computers) {
    }
    
    public override ISet<Type> EventTypes => new HashSet<Type> { typeof(ReturnedToTitleEvent) };
    
    protected override IComputerEvent CreateEvent(IEvent @event) {
        return new StopComputerEvent();
    }
}
