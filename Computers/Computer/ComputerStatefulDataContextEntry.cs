using Computers.Computer.Boundary;
using Computers.Computer.Utils;
using Computers.Core;
using Computers.Game.Boundary;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using StardewModdingAPI;
using Context = Computers.Core.Context;

namespace Computers.Computer;

public class ComputerStatefulDataContextEntry : IContextEntry.StatefulDataContextEntry<ComputerStatefulDataContextEntry>, IComputerPort {
    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly ITargetLoader<string> _entryPointLoader;
    private readonly List<IComputerApi> _computerApis;
    
    private readonly Script _script = new(
        CoreModules.Basic 
        | CoreModules.GlobalConsts
        | CoreModules.TableIterators
        | CoreModules.Metatables
        | CoreModules.String
        | CoreModules.LoadMethods
        | CoreModules.Table
        | CoreModules.ErrorHandling
        | CoreModules.Math
        | CoreModules.Coroutine
        | CoreModules.Bit32
        | CoreModules.Dynamic
        | CoreModules.Json
    );
    
    private readonly InterruptDebugger _interruptDebugger = new();
    private readonly Thread _computerThread;
    
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
        
        _script.AttachDebugger(_interruptDebugger);
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

    public bool Exists(string variableName) {
        return _script.Globals.Get(variableName) != null;
    }

    public void Call(string functionName, params object[] args) {
        _script.Call(_script.Globals[functionName], args);
    }

    public void Set(string variableName, object value) {
        _script.Globals[variableName] = value;
    }

    public T Get<T>(string variableName) {
        return _script.Globals.Get(variableName).ToObject<T>();
    }

    public void Reload() {
        _script.DoString(_entryPointLoader.Load());
        _computerApis.ForEach(RegisterApi);
    }
    
    public void Start() {
        _interruptDebugger.Resume();
        _computerThread.Start();
    }
    
    public void Stop() {
        _interruptDebugger.Interrupt();
        _computerThread.Interrupt();
        _computerThread.Join();
    }
    
    private void RegisterApi(IComputerApi api) {
        api.Reset();
                
        if (!api.ShouldExpose) {
            return;
        }
                
        api.RegisterableApiTypes
            .ForEach(apiType => {
                UserData.RegisterType(apiType);
            });

        _script.Globals[api.Name] = api.Api;
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
                if (e is DebuggerInterruptException) {
                    _monitor.Log("Interrupt occured. Computer thread will be stopped.");
                    break;
                }
                
                if (e is not ScriptRuntimeException scriptRuntimeException) {
                    throw;
                }
                
                _monitor.Log($"Script exception occured: {scriptRuntimeException.DecoratedMessage}");
                if (!_configuration.ShouldResetScriptOnFatalError) {
                    break;
                }
            }
        }
    }
}

internal class DebuggerInterruptException : Exception {
}

internal class InterruptDebugger : IDebugger {
    
    private volatile bool _interrupted;
    
    public void Interrupt() {
        _interrupted = true;
    }
    
    public void Resume() {
        _interrupted = false;
    }

    DebuggerCaps IDebugger.GetDebuggerCaps() {
        return DebuggerCaps.CanDebugByteCode;
    }

    void IDebugger.SetDebugService(DebugService debugService) {
    }

    void IDebugger.SetSourceCode(SourceCode sourceCode) {
    }

    void IDebugger.SetByteCode(string[] byteCode) {
    }

    bool IDebugger.IsPauseRequested() {
        if (_interrupted) {
            throw new DebuggerInterruptException();
        }
        
        return _interrupted;
    }

    bool IDebugger.SignalRuntimeException(ScriptRuntimeException ex) {
        return true;
    }

    DebuggerAction IDebugger.GetAction(int ip, SourceRef sourceref) {
        return new DebuggerAction { Action = DebuggerAction.ActionType.Run };
    }

    void IDebugger.SignalExecutionEnded() {
    }

    void IDebugger.Update(WatchType watchType, IEnumerable<WatchItem> items) {
    }

    List<DynamicExpression> IDebugger.GetWatchItems() {
        return new List<DynamicExpression>();
    }

    void IDebugger.RefreshBreakpoints(IEnumerable<SourceRef> refs) {
    }
}
