using Computers.Core;

namespace Computers.Computer;

public interface IComputerPort {
    Id Id { get; }
    Configuration Configuration { get; }
    Random Random { get; }

    // Assets
    T LoadAsset<T>(string assetPath) where T : notnull;
    
    // Engine
    void Fire(IComputerEvent computerEvent);
    void Set(string variableName, object value);
    T? Get<T>(string variableName);
    object? LoadModule(string moduleName);
    void ProcessTasks();
    
    // Storage
    IDictionary<string, object> GetStorage(IComputerApi api);
    
    // Lifecycle
    void Reload();
    void Start();
    void Stop();
}
