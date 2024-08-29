using Computers.Computer.Boundary;

namespace Computers.Computer;

public class EventComputerApi : IComputerApi {
    public string Name => "Event";
    public bool ShouldExpose => true;
    public object Api => this;
    public List<Type> ReceivableEvents { get; }
    
    public void ReceiveEvent(IComputerEvent computerEvent) {
        throw new NotImplementedException();
    }

    public void Reset() {
        throw new NotImplementedException();
    }
}