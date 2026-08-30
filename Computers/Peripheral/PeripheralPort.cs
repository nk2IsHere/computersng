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

    // The world changed near this peripheral. Cached world-derived state should be dropped.
    void NotifyWorldChanged();
}
