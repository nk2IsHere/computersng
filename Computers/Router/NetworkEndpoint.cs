using Computers.Core;
using Computers.Router.Domain;

namespace Computers.Router;

public interface INetworkEndpoint {
    Id Id { get; }
    void ReceiveDatagram(Datagram datagram);
}
