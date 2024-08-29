using Computers.Computer.Boundary;

namespace Computers.Computer;

public class TickComputerApi: IComputerApi {
    public string Name => "Tick";
    public bool ShouldExpose => false;
    public object Api => this;
    public List<Type> ReceivableEvents => new() { typeof(TickComputerEvent) };

    private readonly Configuration _configuration;
    private readonly IComputerPort _computerPort;

    public TickComputerApi(Configuration configuration, IComputerPort computerPort) {
        _configuration = configuration;
        _computerPort = computerPort;
    }

    public void ReceiveEvent(IComputerEvent computerEvent) {
        _computerPort.Call("Tick", computerEvent.Data<uint>());
    }

    public void Reset() {
        
    }
}
