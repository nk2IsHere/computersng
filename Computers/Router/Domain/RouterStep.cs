using Computers.Core;

namespace Computers.Router.Domain;

public record Delivery(Id EndpointId, Datagram Datagram);

public record Forward(Id TargetRouterId, Datagram Datagram);

public record RouterStepResult(
    IReadOnlyList<Delivery> Deliveries,
    IReadOnlyList<Forward> Forwards,
    IReadOnlyList<Datagram> RouterInbound
);

public static class RouterStep {
    public const string BroadcastAddress = "*";

    public static RouterStepResult Process(
        Id routerId,
        IReadOnlyList<InboxEntry> inbox,
        SeenMessageCache seen,
        INetworkTopology topology
    ) {
        var deliveries = new List<Delivery>();
        var forwards = new List<Forward>();
        var routerInbound = new List<Datagram>();

        foreach (var (datagram, arrivedFrom) in inbox) {
            if (!seen.Remember(datagram.MessageId)) {
                continue;
            }

            if (datagram.TargetAddress == routerId.Last) {
                routerInbound.Add(datagram);
                continue;
            }

            var isBroadcast = datagram.TargetAddress == BroadcastAddress;
            if (isBroadcast) {
                routerInbound.Add(datagram);
            }
            else {
                var targetId = topology.FindEndpointByAddress(datagram.TargetAddress);
                if (targetId is not null && topology.EndpointsCoveredBy(routerId).Contains(targetId)) {
                    deliveries.Add(new Delivery(targetId, datagram));
                    continue;
                }
            }

            if (datagram.Ttl <= 1) {
                continue;
            }

            var forwarded = datagram with { Ttl = datagram.Ttl - 1 };
            topology.Neighbors(routerId)
                .Where(neighborId => neighborId != arrivedFrom)
                .ForEach(neighborId => forwards.Add(new Forward(neighborId, forwarded)));
        }

        return new RouterStepResult(deliveries, forwards, routerInbound);
    }
}
