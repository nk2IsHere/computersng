namespace Computers.Computer.Boundary;

public interface IComputerApi {
    public string Name { get; }
    public bool ShouldExpose { get; }
    public object Api { get; }
    public List<Type> ReceivableEvents { get; }
    public void ReceiveEvent(IComputerEvent computerEvent);
    public void Reset();
}
