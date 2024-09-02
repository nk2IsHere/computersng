using Computers.Computer.Boundary;
using Computers.Core;
using Computers.Game.Boundary;

namespace Computers.Computer;

public class ComputerStartDispatcher: ComputerEventHandler {
    
    public ComputerStartDispatcher(ContextLookup<IComputerPort> computers) : base(computers) {
    }
    
    public override ISet<Type> EventTypes => new HashSet<Type> { typeof(SaveLoadedEvent) };
    
    protected override IComputerEvent CreateEvent(IEvent @event) {
        return new StartComputerEvent();
    }
}
