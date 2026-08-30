using System.Collections.Concurrent;
using Computers.Computer;
using Computers.Core;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using Context = Computers.Core.Context;

namespace Computers.Router.Domain;

public class RouterStatefulDataContextEntry : IContextEntry.StatefulDataContextEntry<RouterStatefulDataContextEntry>, IRouterPort {
    private const int SeenCacheCapacity = 1024;

    private readonly IMonitor _monitor;
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<INetworkEndpoint> _endpoints;
    private readonly ContextLookup<IRouterPort> _routers;

    private readonly ConcurrentQueue<InboxEntry> _inbox = new();
    private readonly SeenMessageCache _seen = new(SeenCacheCapacity);

    private volatile bool _isEnabled = false;

    public RouterStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<INetworkEndpoint> endpoints,
        ContextLookup<IRouterPort> routers
    ) : base(factoryId, id) {
        _monitor = monitor;
        Configuration = configuration;
        _registry = registry;
        _endpoints = endpoints;
        _routers = routers;
    }

    public Configuration Configuration { get; }

    public int? Channel { get; private set; }

    public bool IsEnabled => _isEnabled;

    public override object GetValue(Context context) {
        return this;
    }

    public override void Restore(Context context, ContextEntryState state) {
        var channel = state.GetOrDefault<object?>("Channel", null);
        Channel = channel is null ? null : Convert.ToInt32(channel);
    }

    public override ContextEntryState Store(Context context) {
        var state = ContextEntryState.Empty;

        state.Id = Id;
        state.FactoryId = FactoryId;

        if (Channel is not null) {
            state.Set("Channel", Channel.Value);
        }

        return state;
    }

    public void Start() {
        _monitor.Log($"Router {Id} started");
        _isEnabled = true;
    }

    public void Stop() {
        _monitor.Log($"Router {Id} stopped");
        _isEnabled = false;
        _inbox.Clear();
        _seen.Clear();
    }

    public void Fire(IRouterEvent routerEvent) {
        if (routerEvent is TickRouterEvent) {
            Tick();
        }

        if (routerEvent is StopRouterEvent) {
            Stop();
        }

        if (routerEvent is StartRouterEvent) {
            Start();
        }
    }

    public void Deliver(Datagram datagram, Id? fromRouterId) {
        if (!_isEnabled) {
            return;
        }

        if (_inbox.Count >= Configuration.Network.RouterQueueLimit) {
            _monitor.Log($"Router {Id}: inbox full, dropping datagram {datagram.MessageId}");
            return;
        }

        _inbox.Enqueue(new InboxEntry(datagram, fromRouterId));
    }

    private void Tick() {
        if (!_isEnabled || _inbox.IsEmpty) {
            return;
        }

        var drained = new List<InboxEntry>();
        while (_inbox.TryDequeue(out var entry)) {
            drained.Add(entry);
        }

        var result = RouterStep.Process(Id, drained, _seen, _registry);

        foreach (var delivery in result.Deliveries) {
            var endpoint = _endpoints
                .Get()
                .FirstOrDefault(entry => entry.Id == delivery.EndpointId)
                ?.Value;
            endpoint?.ReceiveDatagram(delivery.Datagram);
        }

        foreach (var forward in result.Forwards) {
            var router = _routers
                .Get()
                .FirstOrDefault(entry => entry.Id == forward.TargetRouterId)
                ?.Value;
            router?.Deliver(forward.Datagram, Id);
        }

        foreach (var inbound in result.RouterInbound) {
            HandleFirmwareRequest(inbound);
        }
    }

    private void HandleFirmwareRequest(Datagram datagram) {
        JObject request;
        try {
            request = JObject.Parse(datagram.Payload);
        } catch (JsonReaderException) {
            _monitor.Log($"Router {Id}: dropped undecodable datagram from {datagram.SourceAddress}.");
            return;
        }

        if (!WireRequests.TryReadCid(request, out var cid)) {
            _monitor.Log($"Router {Id}: dropped datagram without correlation id from {datagram.SourceAddress}.");
            return;
        }

        Reply reply;
        try {
            reply = Reply.Success(cid, Dispatch(RouterRequestParser.ParseBody(request)));
        } catch (RouterRequestException exception) {
            reply = Reply.Failure(cid, exception.Message);
        }

        // The reply enters our own inbox and floods out next tick.
        // That keeps hop counting symmetric with the request's path.
        Deliver(
            new Datagram(
                Guid.NewGuid(),
                Id.Last,
                datagram.SourceAddress,
                Configuration.Network.MessageTtl,
                WireJson.Serialize(reply)
            ),
            null
        );
    }

    private object? Dispatch(RouterRequest request) {
        switch (request) {
            case RouterPingRequest:
                return new RouterPingResult("router", Channel);

            case RouterDiscoverRequest:
                return new RouterDiscoverResult(
                    Id.Last,
                    _registry.EndpointsCoveredBy(Id)
                        .Select(endpointId => endpointId.Last)
                        .OrderBy(address => address)
                        .ToList()
                );

            case RouterConfigureRequest configure:
                Channel = configure.Channel;
                _registry.Invalidate();
                return null;

            default:
                throw new RouterRequestException($"unknown command '{request.GetType().Name}'");
        }
    }
}
