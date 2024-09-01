namespace Computers.Computer.Boundary;

public interface IComputerPort {
    string Id { get; }
    void Fire(IComputerEvent computerEvent);
    void Call(string functionName, params object[] args);
    void Set(string variableName, object value);
    T? Get<T>(string variableName);
    void Reload();
    void Start();
    void Stop();
}
