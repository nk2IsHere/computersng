using Computers.Computer.Boundary;
using Computers.Computer.Utils;
using Computers.Core;
using Computers.Game.Boundary;
using MoonSharp.Interpreter;
using Context = Computers.Core.Context;

namespace Computers.Computer;

public class ComputerStatefulDataContextEntry : IContextEntry.StatefulDataContextEntry<ComputerStatefulDataContextEntry>, IComputerPort {
    private readonly Configuration _configuration;
    private readonly ITargetLoader<string> _entryPointLoader;
    private readonly List<IComputerApi> _computerApis;
    
    private readonly Script _script = new(CoreModules.Preset_SoftSandbox);
    
    public ComputerStatefulDataContextEntry(
        string id,
        ITargetLoader<string> entryPointLoader,
        Configuration configuration,
        BmFont font
    ) : base(id) {
        _configuration = configuration;
        _entryPointLoader = entryPointLoader;
        _computerApis = new List<IComputerApi> {
            new EntryComputerApi(this, _configuration),
            new RenderComputerApi(_configuration, font)
        };
        
        ResetScriptWith(_entryPointLoader.Load());
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

    private void ResetScriptWith(string script) {
        _script.DoString(script);
        _computerApis
            .ForEach(api => {
                api.Reset();
                if (!api.ShouldExpose) {
                    return;
                }
                
                UserData.RegisterType(api.Api.GetType());
                _script.Globals[api.Name] = api.Api;
            });
    }

    public void Fire(IComputerEvent computerEvent) {
        _computerApis
            .ForEach(api => {
                if (api.ReceivableEvents.Contains(computerEvent.GetType())) {
                    api.ReceiveEvent(computerEvent);
                }
            });
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

    public void Reset() {
        _computerApis
            .ForEach(api => {
                api.Reset();
            });
    }
}
