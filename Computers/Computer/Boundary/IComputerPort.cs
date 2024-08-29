namespace Computers.Computer.Boundary;

public interface IComputerPort {
    string Id { get; }
    void Fire(IComputerEvent computerEvent);
    bool Exists(string variableName);
    void Call(string functionName, params object[] args);
    void Set(string variableName, object value);
    T Get<T>(string variableName);
    void Reset();
}
