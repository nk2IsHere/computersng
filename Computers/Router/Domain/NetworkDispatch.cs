using Computers.Core;

namespace Computers.Router.Domain;

public static class NetworkDispatch {
    public static bool Send(
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        Id sourceId,
        string targetAddress,
        string payload,
        int ttl
    ) {
        var coveringIds = registry.RoutersCovering(sourceId);
        if (coveringIds.Count == 0) {
            return false;
        }

        var datagram = new Datagram(Guid.NewGuid(), sourceId.Last, targetAddress, ttl, payload);
        routers
            .Get()
            .Where(entry => coveringIds.Contains(entry.Id))
            .ForEach(entry => entry.Value.Deliver(datagram, null));
        return true;
    }
}
