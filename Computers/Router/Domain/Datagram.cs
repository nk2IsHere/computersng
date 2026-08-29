using Computers.Core;

namespace Computers.Router.Domain;

public record Datagram(
    Guid MessageId,
    string SourceAddress,
    string TargetAddress,
    int Ttl,
    string Payload
);

public record InboxEntry(Datagram Datagram, Id? ArrivedFromRouterId);
