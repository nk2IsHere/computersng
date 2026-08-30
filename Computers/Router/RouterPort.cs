using Computers.Computer;
using Computers.Peripheral;
using Computers.Core;
using Computers.Router.Domain;

namespace Computers.Router;

public interface IRouterPort {
    Id Id { get; }
    Configuration Configuration { get; }

    int? Channel { get; }
    bool IsEnabled { get; }

    // Lifecycle
    void Start();
    void Stop();
    void Fire(IRouterEvent routerEvent);

    // Networking
    void Deliver(Datagram datagram, Id? fromRouterId);
}
