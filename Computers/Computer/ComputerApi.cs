using Computers.Game;

namespace Computers.Computer;

public interface IComputerApi {
    public string Name { get; }
    public bool ShouldExpose { get; }
    public object Api { get; }
    public ISet<Type> ReceivableEvents { get; }
    public IRedundantLoader? LibraryLoader { get; }
    
    public void ReceiveEvent(IComputerEvent computerEvent);
    public void Reset();
}
