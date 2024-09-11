using System.Collections.Concurrent;
using Computers.Computer.Domain.Storage;
using Computers.Core;
using Computers.Game;
using Jint;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Modules;
using StardewModdingAPI;
using Context = Computers.Core.Context;

namespace Computers.Computer.Domain;

public class ComputerStatefulDataContextEntry : IContextEntry.StatefulDataContextEntry<ComputerStatefulDataContextEntry>, IComputerPort {
    private readonly IMonitor _monitor;
    private readonly IRedundantLoader _assetLoader;
    
    private readonly List<IComputerApi> _computerApis;
    
    private Thread? _computerThread;
    private Engine? _engine;
    private ObjectInstance? _entryPointModule;
    private CancellationTokenSource? _cancellationTokenSource;
    
    private readonly IDictionary<string, object> _storage = new ConcurrentDictionary<string, object>();

    public ComputerStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        IRedundantLoader coreLibraryLoader,
        IRedundantLoader assetLoader
    ) : base(factoryId, id) {
        _monitor = monitor;
        Configuration = configuration;
        _assetLoader = assetLoader;
        
        _computerApis = new List<IComputerApi> {
            new RenderComputerApi(this),
            new EventComputerApi(this),
            new SystemComputerApi(this),
            new StorageComputerApi(
                this,
                api => {
                    var storageLayers = new List<IStorageLayer> {
                        new LoaderStorageLayer(coreLibraryLoader)
                    };
        
                    if (configuration.Storage.EnablePersistentStorage) {
                        storageLayers.Add(new PersistentStorageLayer(GetStorage(api)));
                    }
        
                    if (configuration.Storage.EnableExternalStorage) {
                        storageLayers.Add(new LoaderStorageLayer(new ComputerLoader(this)));
                    }
                    
                    return storageLayers;
                }
            ),
            new NetworkComputerApi(this)
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

        if (computerEvent is StartComputerEvent) {
            Start();
        }
    }

    public void Set(string variableName, object value) {
        _engine?.SetValue(variableName, value);
    }

    public T? Get<T>(string variableName) {
        return (T?) _engine?.GetValue(variableName).ToObject();
    }

    public object? LoadModule(string moduleName) {
        return _engine?.Modules.Import(moduleName);
    }

    public void ProcessTasks() {
        _engine?.Advanced.ProcessTasks();
    }

    public IDictionary<string, object> GetStorage(IComputerApi api) {
        if(_storage.TryGetValue(api.Name, out var value)) {
            return (IDictionary<string, object>) value;
        }
        
        var storage = new ConcurrentDictionary<string, object>();
        _storage.Add(api.Name, storage);
        return storage;
    }

    public void Reload() {
        _cancellationTokenSource = new CancellationTokenSource();
        _engine?.Dispose();

        var libraryLoaders = _computerApis
            .Select(api => api.LibraryLoader)
            .OfType<IRedundantLoader>()
            .ToList();
        
        _engine = new Engine(
            options => {
                options.Strict();
                options.CancellationToken(_cancellationTokenSource.Token);
                options.EnableModules(new ComputerModuleLoader(_monitor, libraryLoaders));
            }
        );

        _computerApis.ForEach(RegisterApi);
        _entryPointModule = _engine.Modules.Import(Configuration.Resource.EntryPointModule);
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
                if (!Configuration.Engine.ShouldResetScriptOnFatalError) {
                    break;
                }
            }
        }
    }
}

internal class ComputerModuleLoader : ModuleLoader {

    private readonly IMonitor _monitor;
    private readonly List<IRedundantLoader> _libraryLoaders;
    
    public ComputerModuleLoader(IMonitor monitor, List<IRedundantLoader> libraryLoaders) {
        _monitor = monitor;
        _libraryLoaders = libraryLoaders;
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
        foreach (var libraryModule in _libraryLoaders.Select(loader => TryLoadModuleUsing(loader, fileName)).OfType<string>()) {
            return libraryModule;
        }
        
        throw new InvalidOperationException($"Module {fileName} not found.");
    }
    
    private static string? TryLoadModuleUsing(IRedundantLoader loader, string fileName) {
        if (loader.Exists(fileName)) {
            return loader.Load<string>(fileName);
        }
        
        var fileNameWithExtension = fileName + ".js";
        return loader.Exists(fileNameWithExtension) 
            ? loader.Load<string>(fileNameWithExtension)
            : null;
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

internal class ComputerLoader : IRedundantLoader {
    private readonly IComputerPort _computerPort;

    public ComputerLoader(IComputerPort computerPort) {
        _computerPort = computerPort;
    }

    public T Load<T>(string path) where T : notnull {
        throw new NotImplementedException();
    }

    public IEnumerable<FileSystemEntry> List(string path) {
        throw new NotImplementedException();
    }

    public bool Exists(string path) {
        throw new NotImplementedException();
    }
}
