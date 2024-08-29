using Computers.Computer.Boundary;

namespace Computers.Computer;

public class EntryComputerApi : IComputerApi {
    public string Name => "Entry";
    public bool ShouldExpose => false;
    public object Api => this;
    public List<Type> ReceivableEvents => new();

    private readonly IComputerPort _computerPort;
    
    private Thread? _computerThread;

    public EntryComputerApi(IComputerPort computerPort) {
        _computerPort = computerPort;
    }

    public void ReceiveEvent(IComputerEvent computerEvent) {
        throw new InvalidOperationException("Entry Computer API does not receive events.");
    }

    public void Reset() {
        _computerThread?.Interrupt();
        
        if (!_computerThread?.Join(10) ?? false) {
            throw new InvalidOperationException("Failed to stop the computer thread.");
        }

        if (!_computerPort.Exists("Entry")) {
            return;
        }
        
        _computerThread = new Thread(() => {
            var shouldRun = true;
            while (shouldRun) {
                try {
                    _computerPort.Call("Entry");
                } catch(Exception e) {
                    Console.WriteLine(e);
                    if (e is ThreadInterruptedException) {
                        shouldRun = false;
                    }
                }
            }
        });
        
        _computerThread.Start();
    }
}
