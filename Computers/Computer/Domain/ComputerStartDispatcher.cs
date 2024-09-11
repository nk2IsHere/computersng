using Computers.Core;
using Computers.Game;

namespace Computers.Computer.Domain;

public class ComputerStartDispatcher: ComputerEventHandler {
    
    public ComputerStartDispatcher(ContextLookup<IComputerPort> computers) : base(computers) {
    }
    
    public override ISet<Type> EventTypes => new HashSet<Type> { typeof(SaveLoadedEvent) };
    
    protected override IComputerEvent CreateEvent(IEvent @event) {
        return new StartComputerEvent();
    }
}
