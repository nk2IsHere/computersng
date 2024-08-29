namespace Computers.Computer.Boundary;

public interface IComputerPort {
    void Fire(IComputerEvent computerEvent);
    void Call(string functionName, params object[] args);
    void Set(string variableName, object value);
    T Get<T>(string variableName);
    void Reset();
}
