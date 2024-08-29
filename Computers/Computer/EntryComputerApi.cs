using System.Diagnostics.CodeAnalysis;
using Computers.Computer.Boundary;

namespace Computers.Computer;

public class EntryComputerApi : IComputerApi {
    public string Name => "Entry";
    public bool ShouldExpose => false;
    public object Api => this;
    public List<Type> ReceivableEvents => new();

    private readonly IComputerPort _computerPort;
    private readonly Configuration _configuration;
    
    private Thread? _computerThread;

    public EntryComputerApi(IComputerPort computerPort, Configuration configuration) {
        _computerPort = computerPort;
        _configuration = configuration;
    }

    public void ReceiveEvent(IComputerEvent computerEvent) {
        throw new InvalidOperationException("Entry Computer API does not receive events.");
    }

    [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: MoonSharp.Interpreter.DynValue")]
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
                    if (!_configuration.ShouldResetScriptOnFatalError) {
                        shouldRun = false;
                    }
                    
                    if (e is ThreadInterruptedException) {
                        shouldRun = false;
                    }
                    
                    Console.WriteLine(e);
                }
            }
        });
        
        _computerThread.Start();
    }
}
