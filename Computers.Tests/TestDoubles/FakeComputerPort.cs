using Computers.Computer;
using Computers.Core;

namespace Computers.Tests.TestDoubles;

public class FakeComputerPort : IComputerPort {
    public FakeComputerPort(Id id) {
        Id = id;
    }

    public Id Id { get; }
    public Configuration Configuration { get; set; } = new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>()
        }
    };
    public Random Random { get; } = new(42);

    public List<IComputerEvent> FiredEvents { get; } = new();

    public T LoadAsset<T>(string assetPath) where T : notnull => throw new NotSupportedException();
    public void Fire(IComputerEvent computerEvent) => FiredEvents.Add(computerEvent);
    public void Set(string variableName, object value) { }
    public T? Get<T>(string variableName) => default;
    public object? LoadModule(string moduleName) => null;
    public void ProcessTasks() { }
    public IDictionary<string, object> GetStorage(IComputerApi api) => new Dictionary<string, object>();
    public void Reload() { }
    public void Start() { }
    public void Stop() { }
}
