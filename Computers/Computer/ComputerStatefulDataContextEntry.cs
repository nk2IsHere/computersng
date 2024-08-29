using Computers.Computer.Boundary;
using Computers.Core;
using Computers.Game.Boundary;
using Microsoft.Xna.Framework.Graphics;
using MoonSharp.Interpreter;
using Context = Computers.Core.Context;

namespace Computers.Computer;

public class ComputerStatefulDataContextEntry : IContextEntry.StatefulDataContextEntry<ComputerStatefulDataContextEntry>, IComputerState {
    private readonly bool _shouldReloadScriptEveryTick;
    private readonly ITargetLoader<string> _entryPointLoader;
    private readonly Script _script = new(CoreModules.Preset_SoftSandbox);
    
    public ComputerStatefulDataContextEntry(
        string id,
        ITargetLoader<string> entryPointLoader,
        bool shouldReloadScriptEveryTick
    ) : base(id) {
        _entryPointLoader = entryPointLoader;
        _shouldReloadScriptEveryTick = shouldReloadScriptEveryTick;
        
        _script.DoString(_entryPointLoader.Load());
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
    
    public void Tick(int ticks) {
        if (_shouldReloadScriptEveryTick) {
            _script.DoString(_entryPointLoader.Load());
        }
            
        if (_script.Globals["Tick"] != null) {
            _script.Call(_script.Globals["Tick"], ticks);
        }
    }

    public void Render(SpriteBatch batch) {
        throw new NotImplementedException();
    }
}
