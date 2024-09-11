using Computers.Game;

namespace Computers.Computer.Domain;

public class SystemComputerApi : IComputerApi {
    public string Name => "System";
    public bool ShouldExpose => true;
    public object Api => _state;
    
    public ISet<Type> ReceivableEvents => new HashSet<Type>();
    public IRedundantLoader? LibraryLoader => null;


    private readonly IComputerPort _computerPort;
    
    private readonly SystemComputerState _state;

    public SystemComputerApi(IComputerPort computerPort) {
        _computerPort = computerPort;
        _state = new SystemComputerState(_computerPort);
    }
    
    public void ReceiveEvent(IComputerEvent computerEvent) {
    }

    public void Reset() {
    }
}

internal class SystemComputerState {
    
    private readonly IComputerPort _computerPort;
    
    public SystemComputerState(IComputerPort computerPort) {
        _computerPort = computerPort;
    }
    
    public void Sleep(int milliseconds) {
        if (milliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "Sleep time cannot be negative");
        }
        
        Thread.Sleep(milliseconds);
    }
    
    public long Time() {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    
    public object? LoadModule(string moduleName) {
        return _computerPort.LoadModule(moduleName);
    }
    
    public void ProcessTasks() {
        _computerPort.ProcessTasks();
    }

    public string Id() {
        return _computerPort.Id;
    }
}
