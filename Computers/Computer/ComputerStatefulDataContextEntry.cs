using System.Collections.Concurrent;
using Computers.Computer.Boundary;
using Computers.Core;
using Computers.Game.Boundary;
using Jint;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Modules;
using StardewModdingAPI;
using Context = Computers.Core.Context;

namespace Computers.Computer;

public class ComputerStatefulDataContextEntry : IContextEntry.StatefulDataContextEntry<ComputerStatefulDataContextEntry>, IComputerPort {
    private readonly IMonitor _monitor;
    private readonly IRedundantLoader _coreLibraryLoader;
    private readonly IRedundantLoader _assetLoader;
    
    private readonly List<IComputerApi> _computerApis;
    
    private Thread? _computerThread;
    private Engine? _engine;
    private ObjectInstance? _entryPointModule;
    private CancellationTokenSource? _cancellationTokenSource;
    
    private IDictionary<string, object> _storage = new ConcurrentDictionary<string, object>();

    public ComputerStatefulDataContextEntry(
        string factoryId,
        string id,
        IMonitor monitor,
        Configuration configuration,
        IRedundantLoader coreLibraryLoader,
        IRedundantLoader assetLoader
    ) : base(factoryId, id) {
        _monitor = monitor;
        Configuration = configuration;
        _coreLibraryLoader = coreLibraryLoader;
        _assetLoader = assetLoader;
        
        _computerApis = new List<IComputerApi> {
            new RenderComputerApi(this),
            new EventComputerApi(this),
            new SystemComputerApi(this)
        };
        
        Reload();
    }

    public override object GetValue(Context context) {
        return this;
    }

    public override void Restore(Context context, ContextEntryState state) {
        var computerState = state.GetOrDefault("Storage", new Dictionary<string, object>());
        
        _storage.Clear();
        computerState.ForEach(pair => _storage.Add(pair.Key, pair.Value));
    }

    public override ContextEntryState Store(Context context) {
        var state = ContextEntryState.Empty;
        
        state.Id = Id;
        state.FactoryId = FactoryId;
        
        state.Set("Storage", _storage.ToDictionary(pair => pair.Key, pair => pair.Value));
        return state;
    }

    public Configuration Configuration { get; }

    public T LoadAsset<T>(string assetPath) where T : notnull {
        return _assetLoader.Load<T>(assetPath);
    }

    public void Fire(IComputerEvent computerEvent) {
        _computerApis
            .Where(api => api.ReceivableEvents.Contains(computerEvent.GetType()))
            .ForEach(api => api.ReceiveEvent(computerEvent));

        if (computerEvent is StopComputerEvent) {
            Stop();
        }
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
                options.EnableModules(new ComputerModuleLoader(Configuration, _monitor, _coreLibraryLoader));
            }
        );

        _computerApis.ForEach(RegisterApi);
        _engine.SetValue("Storage", _storage);
        _entryPointModule = _engine.Modules.Import(Configuration.EntryPointModule);
    }

    public void Start() {
        if (_computerThread?.IsAlive == true) {
            return;
        }
        
        _computerThread = new Thread(ComputerThreadBody);
        _computerThread.Start();
    }

    public void Stop() {
        if (_computerThread is null || !_computerThread.IsAlive) {
            return;
        }
        
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

                if (_entryPointModule == null) {
                    break;
                }
                
                _entryPointModule.Get("Main").Call();
            }
            catch (Exception e) {
                if (e is ExecutionCanceledException) {
                    _monitor.Log("Interrupt occured. Computer thread will be stopped.");
                    break;
                }

                if (e is not JavaScriptException javaScriptException) {
                    _monitor.Log($"Exception occured: {e}");
                    break;
                }

                _monitor.Log($"Script exception occured: {javaScriptException}");
                if (!Configuration.ShouldResetScriptOnFatalError) {
                    break;
                }
            }
        }
    }
}

internal class ComputerModuleLoader : ModuleLoader {

    private readonly Configuration _configuration;
    private readonly IMonitor _monitor;
    private readonly IRedundantLoader _coreLibraryLoader;
    
    public ComputerModuleLoader(Configuration configuration, IMonitor monitor, IRedundantLoader coreLibraryLoader) {
        _configuration = configuration;
        _monitor = monitor;
        _coreLibraryLoader = coreLibraryLoader;
    }

    public override ResolvedSpecifier Resolve(string? referencingModuleLocation, ModuleRequest moduleRequest) {
        _monitor.Log($"Resolving module: module location is {referencingModuleLocation} request is {moduleRequest}");
        
        var specifier = moduleRequest.Specifier;
        if (string.IsNullOrEmpty(specifier)) {
            throw new InvalidOperationException($"Invalid Module Specifier for module request: {moduleRequest}");
        }

        var resolvedUri = new Uri("/");
        if (Uri.TryCreate(specifier, UriKind.Absolute, out var uri)) {
            resolvedUri = uri;
        } else if(IsRelative(specifier)) {
            var baseUri = BuildBaseUri(referencingModuleLocation);
            resolvedUri = new Uri(baseUri, specifier);
        }
        
        return new ResolvedSpecifier(
            moduleRequest,
            resolvedUri.AbsoluteUri,
            resolvedUri,
            SpecifierType.RelativeOrAbsolute
        );
    }

    protected override string LoadModuleContents(Engine engine, ResolvedSpecifier resolved) {
        _monitor.Log($"Loading module: {resolved}");
        if (resolved.Uri is null) {
            throw new InvalidOperationException($"Invalid Module Specifier for module request: {resolved}");
        }
        
        var fileName = Uri.UnescapeDataString(resolved.Uri.AbsolutePath);
        return _coreLibraryLoader.Load<string>(fileName);
    }

    private static bool IsRelative(string specifier) {
        return specifier.StartsWith('.') || specifier.StartsWith('/');
    }
    
    private static Uri BuildBaseUri(string? referencingModuleLocation) {
        if (referencingModuleLocation is null) {
            return new Uri("/");
        }
        
        return Uri.TryCreate(referencingModuleLocation, UriKind.Absolute, out var referencingLocation) 
            ? referencingLocation
            : new Uri("/");
    }
}
