using Computers.Computer;
using Computers.Core;
using Computers.Router;
using Computers.Router.Domain;

namespace Computers.Tests.TestDoubles;

public class FakeRouterPort : IRouterPort {
    public FakeRouterPort(Id id, int? channel = null, bool isEnabled = true) {
        Id = id;
        Channel = channel;
        IsEnabled = isEnabled;
    }

    public Id Id { get; }
    public Configuration Configuration { get; } = new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>()
        }
    };
    public int? Channel { get; set; }
    public bool IsEnabled { get; set; }

    public List<(Datagram Datagram, Id? FromRouterId)> Delivered { get; } = new();

    public void Start() => IsEnabled = true;
    public void Stop() => IsEnabled = false;
    public void Fire(IRouterEvent routerEvent) { }

    public void Deliver(Datagram datagram, Id? fromRouterId) {
        Delivered.Add((datagram, fromRouterId));
    }
}
