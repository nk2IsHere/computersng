using Computers.Computer.Boundary;
using Computers.Core;
using Computers.Game.Boundary;

namespace Computers.Computer;

public class ComputerStopDispatcher: IEventHandler {
       
    private readonly ContextLookup<IComputerPort> _computers;

    public ComputerStopDispatcher(ContextLookup<IComputerPort> computers) {
        _computers = computers;
    }
    
    public ISet<Type> EventTypes => new HashSet<Type>() { typeof(ReturnedToTitleEvent) };
    
    public void Handle(IEvent @event) {
        _computers
            .Get()
            .ForEach(computer => computer.Value.Fire(new StopComputerEvent()));
    }
}
