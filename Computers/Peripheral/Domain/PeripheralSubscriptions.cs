using Newtonsoft.Json.Linq;

namespace Computers.Peripheral.Domain;

// Named event subscriber registry shared by peripheral firmware. Handles the subscribe
// and unsubscribe commands against the peripheral's declared event names.
public class PeripheralSubscriptions {
    private readonly IReadOnlySet<string> _supportedEvents;
    private readonly int _maxSubscribers;
    private readonly Dictionary<string, HashSet<string>> _subscribersByEvent = new();

    public PeripheralSubscriptions(IEnumerable<string> supportedEvents, int maxSubscribers) {
        _supportedEvents = supportedEvents.ToHashSet();
        _maxSubscribers = maxSubscribers;
    }

    public IReadOnlyCollection<string> Subscribers(string eventName) {
        return _subscribersByEvent.GetValueOrDefault(eventName) ?? (IReadOnlyCollection<string>) Array.Empty<string>();
    }

    // Handles a subscribe or unsubscribe request. Returns false when cmd is neither, so
    // the caller dispatches its own commands. Throws PeripheralRequestException on a
    // malformed events list, an unsupported event or a full subscriber set.
    public bool TryHandle(string? cmd, JObject payload, string sourceAddress) {
        if (cmd != "subscribe" && cmd != "unsubscribe") {
            return false;
        }

        var events = ReadEvents(payload);
        foreach (var eventName in events) {
            if (!_supportedEvents.Contains(eventName)) {
                throw new PeripheralRequestException($"unsupported event '{eventName}'");
            }
        }

        foreach (var eventName in events) {
            var subscribers = _subscribersByEvent.TryGetValue(eventName, out var existing)
                ? existing
                : _subscribersByEvent[eventName] = new HashSet<string>();

            if (cmd == "subscribe") {
                if (!subscribers.Contains(sourceAddress) && subscribers.Count >= _maxSubscribers) {
                    throw new PeripheralRequestException("subscriber limit reached");
                }
                subscribers.Add(sourceAddress);
            }
            else {
                subscribers.Remove(sourceAddress);
            }
        }

        return true;
    }

    private static IReadOnlyList<string> ReadEvents(JObject payload) {
        if (payload["events"] is not JArray events || events.Count == 0) {
            throw new PeripheralRequestException("subscribe requires a non-empty events list");
        }

        return events.Select(e => e.Value<string>() ?? throw new PeripheralRequestException("events must be strings")).ToList();
    }
}
