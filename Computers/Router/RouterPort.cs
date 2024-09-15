using Computers.Computer;
using Computers.Core;

namespace Computers.Router;

public interface IRouterPort {
    Id Id { get; }
    Configuration Configuration { get; }

    // Lifecycle
    void Start();
    void Stop();
    void Fire(IRouterEvent routerEvent);
}
