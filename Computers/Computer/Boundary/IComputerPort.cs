namespace Computers.Computer.Boundary;

public interface IComputerPort {
    string Id { get; }
    Configuration Configuration { get; }
    
    // Assets
    T LoadAsset<T>(string assetPath);
    
    // Engine
    void Fire(IComputerEvent computerEvent);
    void Set(string variableName, object value);
    T? Get<T>(string variableName);
    
    // Lifecycle
    void Reload();
    void Start();
    void Stop();
}
