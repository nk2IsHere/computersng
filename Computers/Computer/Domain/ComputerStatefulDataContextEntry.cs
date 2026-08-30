using System.Collections.Concurrent;
using Computers.Computer.Domain.Api;
using Computers.Computer.Domain.Storage;
using Computers.Core;
using Computers.Game;
using Computers.Router;
using Computers.Router.Domain;
using Jint;
using Jint.Native.Object;
using Jint.Runtime;
using StardewModdingAPI;
using StardewValley;
using Context = Computers.Core.Context;

namespace Computers.Computer.Domain;

public class ComputerStatefulDataContextEntry : IContextEntry.StatefulDataContextEntry<ComputerStatefulDataContextEntry>, IComputerPort, ISliceTarget {
    private readonly IMonitor _monitor;
    private readonly IRedundantLoader _assetLoader;
    private readonly ComputerScheduler _scheduler;

    private readonly List<IComputerApi> _computerApis;

    private Engine? _engine;
    private CancellationTokenSource? _cancellationTokenSource;

    private TaskCompletionSource _frameGate = NewGate();
    private int _needsBoot; // 1 = boot on next slice
    private volatile bool _disabled; // fatal error with reset disabled
    private volatile bool _stopping;

    private readonly IDictionary<string, object> _storage = new ConcurrentDictionary<string, object>();

    private static TaskCompletionSource NewGate() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ComputerStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        Random random,
        IRedundantLoader coreLibraryLoader,
        IRedundantLoader assetLoader,
        IRedundantLoader dataLoader,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        ComputerScheduler scheduler
    ) : base(factoryId, id) {
        _monitor = monitor;
        Configuration = configuration;
        Random = random;
        _assetLoader = assetLoader;
        _scheduler = scheduler;
        
        _computerApis = new List<IComputerApi> {
            new RenderComputerApi(this),
            new EventComputerApi(this),
            new SystemComputerApi(this),
            new StorageComputerApi(
                this,
                api => {
                    var storageLayers = new List<IStorageLayer> {
                        new LoaderStorageLayer(coreLibraryLoader, "Core", 1)
                    };
                    
                    if (configuration.Storage.EnableExternalStorage) {
                        storageLayers.Add(new LoaderStorageLayer(new ComputerDataLoader(this, dataLoader), "External"));
                    }
        
                    if (configuration.Storage.EnablePersistentStorage) {
                        storageLayers.Add(new PersistentStorageLayer(GetStorage(api)));
                    }
                    
                    return storageLayers;
                }
            ),
            new NetworkComputerApi(this, registry, routers)
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
    public Random Random { get; }

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

    public Task NextFrame() {
        return _frameGate.Task;
    }

    public void ReceiveDatagram(Datagram datagram) {
        Fire(new NetworkMessageComputerEvent(Id, datagram.MessageId, datagram.SourceAddress, datagram.Payload));
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
                options.ExperimentalFeatures = ExperimentalFeature.All;
                options.CatchClrExceptions();

                if (Configuration.Engine.MaxStatementsPerSlice > 0) {
                    options.MaxStatements(Configuration.Engine.MaxStatementsPerSlice);
                }
            }
        );

        _computerApis.ForEach(RegisterApi);
    }

    public void Start() {
        _stopping = false;
        _disabled = false;
        Interlocked.Exchange(ref _needsBoot, 1);
        _scheduler.Register(this);
    }

    public void Stop() {
        _stopping = true;
        _scheduler.Unregister(Id);
        _cancellationTokenSource?.Cancel();
    }
    
    public void OpenFrameGate() {
        var previous = Interlocked.Exchange(ref _frameGate, NewGate());
        previous.TrySetResult();
    }

    public void RunSlice() {
        if (_disabled || _stopping) {
            return;
        }

        if (Interlocked.Exchange(ref _needsBoot, 0) == 1) {
            Boot();
        }

        ProcessTasks();
    }

    public void OnSliceError(Exception exception) {
        if (exception is ExecutionCanceledException) {
            _monitor.Log($"Computer {Id}: execution canceled (stopping).");
            return;
        }

        if (exception is StatementsCountOverflowException) {
            _monitor.Log(
                $"Computer {Id}: script exceeded the statement budget for a single slice and was aborted. " +
                "Long computations must yield (await System.NextFrame() / System.Delay).",
                LogLevel.Warn
            );
        }
        else {
            _monitor.Log($"Computer {Id}: script error: {exception}", LogLevel.Warn);
        }

        HandleFatalScriptError();
    }

    private void HandleFatalScriptError() {
        if (Configuration.Engine.ShouldResetScriptOnFatalError) {
            Interlocked.Exchange(ref _needsBoot, 1); // reboot on next slice
        }
        else {
            _disabled = true;
            _monitor.Log($"Computer {Id} disabled (shouldResetScriptOnFatalError = false).", LogLevel.Warn);
        }
    }

    private void Boot() {
        Reload();

        Set("__computerOnScriptEnd", new Action(() =>
            _monitor.Log($"Computer {Id}: entrypoint Main completed; computer is idle."))
        );
        Set("__computerOnScriptError", new Action<string>(error => {
            _monitor.Log($"Computer {Id}: script error: {error}", LogLevel.Warn);
            HandleFatalScriptError();
        }));

        var module = LoadModule("/Entrypoint");
        if (module is not ObjectInstance) {
            _monitor.Log($"Computer {Id}: entrypoint module not found; computer disabled.", LogLevel.Error);
            _disabled = true;
            return;
        }

        Set("__computerEntrypoint", module);
        _engine!.Execute(
            "__computerEntrypoint.Main().then(() => __computerOnScriptEnd(), e => __computerOnScriptError(String(e && e.stack || e)))"
        );
    }

    private void RegisterApi(IComputerApi api) {
        api.Reset();

        if (!api.ShouldExpose) {
            return;
        }

        Set(api.Name, api.Api);
    }
}
