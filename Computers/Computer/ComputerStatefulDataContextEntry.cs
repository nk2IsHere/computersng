using Computers.Computer.Boundary;
using Computers.Computer.Utils;
using Computers.Core;
using Computers.Game.Boundary;
using Jint;
using Jint.Constraints;
using Jint.Native;
using Jint.Runtime;
using StardewModdingAPI;
using Context = Computers.Core.Context;

namespace Computers.Computer;

public class
    ComputerStatefulDataContextEntry : IContextEntry.StatefulDataContextEntry<ComputerStatefulDataContextEntry>,
    IComputerPort {
    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly ITargetLoader<string> _entryPointLoader;
    private readonly List<IComputerApi> _computerApis;

    private readonly Thread _computerThread;
    
    private Engine? _engine;
    private CancellationTokenSource? _cancellationTokenSource;

    public ComputerStatefulDataContextEntry(
        string id,
        IMonitor monitor,
        Configuration configuration,
        ITargetLoader<string> entryPointLoader,
        BmFont font
    ) : base(id) {
        _monitor = monitor;
        _configuration = configuration;
        _entryPointLoader = entryPointLoader;
        _computerApis = new List<IComputerApi> {
            new RenderComputerApi(this, _configuration, font),
            new EventComputerApi(this, _configuration),
            new SystemComputerApi(this, _configuration)
        };
        
        _computerThread = new Thread(ComputerThreadBody);
        Reload();
    }

    public override object GetValue(Context context) {
        return this;
    }

    public override void Restore(Context context, ContextEntryState state) {
        throw new NotImplementedException();
    }

    public override ContextEntryState Store(Context context) {
        throw new NotImplementedException();
    }

    public void Fire(IComputerEvent computerEvent) {
        _computerApis
            .Where(api => api.ReceivableEvents.Contains(computerEvent.GetType()))
            .ForEach(api => api.ReceiveEvent(computerEvent));

        if (computerEvent is StopComputerEvent) {
            Stop();
        }
    }

    public void Call(string functionName, params object[] args) {
        _engine?.Call(
            functionName, 
            args
                .Select(arg => JsValue.FromObject(_engine, arg))
                .ToArray()
        );
    }

    public void Set(string variableName, object value) {
        _engine?.SetValue(variableName, value);
    }

    public T? Get<T>(string variableName) {
        return (T?) _engine?.GetValue(variableName).ToObject();
    }

    public void Reload() {
        _cancellationTokenSource = new CancellationTokenSource();
        
        _engine?.Dispose();
        _engine = new Engine(
            options => {
                options.Strict();
                options.CancellationToken(_cancellationTokenSource.Token);
            }
        );
        _engine.Execute(_entryPointLoader.Load());
        _computerApis.ForEach(RegisterApi);
    }

    public void Start() {
        _computerThread.Start();
    }

    public void Stop() {
        _cancellationTokenSource?.Cancel();
        _computerThread.Interrupt();
        _computerThread.Join();
    }

    private void RegisterApi(IComputerApi api) {
        api.Reset();

        if (!api.ShouldExpose) {
            return;
        }
        
        Set(api.Name, api.Api);
    }

    private void ComputerThreadBody() {
        while (true) {
            try {
                lock (this) {
                    Reload();
                }

                Call(_configuration.EntryPointName);
            }
            catch (Exception e) {
                if (e is ExecutionCanceledException) {
                    _monitor.Log("Interrupt occured. Computer thread will be stopped.");
                    break;
                }

                if (e is not JavaScriptException javaScriptException) {
                    throw;
                }

                _monitor.Log($"Script exception occured: {javaScriptException}");
                if (!_configuration.ShouldResetScriptOnFatalError) {
                    break;
                }
            }
        }
    }
}