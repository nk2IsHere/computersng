using Computers.Core;

namespace Computers.Computer.Boundary;

public interface IComputerPort {
    Id Id { get; }
    Configuration Configuration { get; }
    
    // Assets
    T LoadAsset<T>(string assetPath) where T : notnull;
    
    // Engine
    void Fire(IComputerEvent computerEvent);
    void Set(string variableName, object value);
    T? Get<T>(string variableName);
    
    // Storage
    IDictionary<string, object> GetStorage(IComputerApi api);
    
    // Lifecycle
    void Reload();
    void Start();
    void Stop();
}
