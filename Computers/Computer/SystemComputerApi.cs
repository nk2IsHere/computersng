using Computers.Computer.Boundary;

namespace Computers.Computer;

public class SystemComputerApi : IComputerApi {
    public string Name => "System";
    public bool ShouldExpose => true;
    public object Api => _state;
    
    public ISet<Type> ReceivableEvents => new HashSet<Type>();
    
    public ISet<Type> RegisterableApiTypes => new HashSet<Type> { typeof(SystemComputerState) };


    private readonly IComputerPort _computerPort;
    private readonly Configuration _configuration;
    
    private readonly SystemComputerState _state;

    public SystemComputerApi(IComputerPort computerPort, Configuration configuration) {
        _computerPort = computerPort;
        _configuration = configuration;
        _state = new SystemComputerState();
    }
    
    public void ReceiveEvent(IComputerEvent computerEvent) {
    }

    public void Reset() {
    }
}

internal class SystemComputerState {
    
    public void Sleep(int milliseconds) {
        if (milliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "Sleep time cannot be negative");
        }
        
        Thread.Sleep(milliseconds);
    }
    
    public long Time() {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
