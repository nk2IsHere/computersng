using Computers.Computer;
using Computers.Router;

namespace Computers.Peripheral;

public interface IPeripheralPort : INetworkEndpoint {
    Configuration Configuration { get; }
    bool IsEnabled { get; }

    // Lifecycle
    void Start();
    void Stop();
    void Fire(IPeripheralEvent peripheralEvent);

    // Invalidate cached world-derived state (e.g. the machine group) after nearby changes.
    void InvalidateGroup();
}
