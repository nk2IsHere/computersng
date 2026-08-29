using System.Text;
using Computers.Core;
using Computers.Game;
using Computers.Router;
using Computers.Router.Domain;

namespace Computers.Computer.Domain.Api;

public class NetworkComputerApi : IComputerApi {
    public string Name => "Network";
    public bool ShouldExpose => true;
    public object Api => _state;
    
    public ISet<Type> ReceivableEvents => new HashSet<Type>();
    public IRedundantLoader? LibraryLoader => null;

    private readonly NetworkComputerState _state;

    public NetworkComputerApi(
        IComputerPort computerPort,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers
    ) {
        _state = new NetworkComputerState(computerPort, registry, routers);
    }

    public void ReceiveEvent(IComputerEvent computerEvent) {
    }

    public void Reset() {
    }
}

internal record HttpResponseBytes(
    int StatusCode,
    IDictionary<string, string> Headers,
    byte[] Body
);

internal record HttpResponseString(
    int StatusCode,
    IDictionary<string, string> Headers,
    string Body
);

internal class NetworkComputerState {

    private readonly Configuration _configuration;
    private readonly IComputerPort _computerPort;
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<IRouterPort> _routers;
    private readonly HttpClient _client = new();

    public NetworkComputerState(
        IComputerPort computerPort,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers
    ) {
        _configuration = computerPort.Configuration;
        _computerPort = computerPort;
        _registry = registry;
        _routers = routers;
    }

    public void SendMessage(string address, string payload) {
        if (Encoding.UTF8.GetByteCount(payload) > _configuration.Network.MaxPayloadBytes) {
            throw new InvalidOperationException(
                $"Payload exceeds maximum size of {_configuration.Network.MaxPayloadBytes} bytes");
        }

        var coveringRouters = CoveringRouterPorts();
        if (coveringRouters.Count == 0) {
            throw new InvalidOperationException("No router in range - computer is offline");
        }

        var datagram = new Datagram(
            Guid.NewGuid(),
            GetAddress(),
            address,
            _configuration.Network.MessageTtl,
            payload
        );

        coveringRouters.ForEach(router => router.Deliver(datagram, null));
    }

    public string GetAddress() {
        return _computerPort.Id.Last;
    }

    public string[] ListReachable() {
        return _registry
            .ReachableComputers(_computerPort.Id)
            .Select(id => id.Last)
            .OrderBy(address => address)
            .ToArray();
    }

    public List<Dictionary<string, object?>> GetRouters() {
        return CoveringRouterPorts()
            .Select(router => new Dictionary<string, object?> {
                ["address"] = router.Id.Last,
                ["channel"] = router.Channel
            })
            .ToList();
    }

    public void ConfigureRouter(string routerAddress, int? channel) {
        var router = CoveringRouterPorts().FirstOrDefault(port => port.Id.Last == routerAddress);
        if (router is null) {
            throw new InvalidOperationException($"No covering router with address '{routerAddress}'");
        }

        router.Channel = channel;
        _registry.Invalidate();
    }

    private List<IRouterPort> CoveringRouterPorts() {
        var coveringIds = _registry.RoutersCovering(_computerPort.Id);
        return _routers
            .Get()
            .Where(entry => coveringIds.Contains(entry.Id))
            .Select(entry => entry.Value)
            .ToList();
    }

    public async ValueTask<HttpResponseBytes> RequestHttpBytes(
        string url,
        string method,
        IDictionary<string, object>? headers,
        byte[]? body
    ) {
        EnsureUrlIsAllowed(url);

        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (headers != null) {
            foreach (var header in headers) {
                request.Headers.Add(header.Key, header.Value.ToString());
            }
        }

        if (body != null) {
            request.Content = new ByteArrayContent(body);
        }

        var response = await _client.SendAsync(request);
        var responseBody = await response.Content.ReadAsByteArrayAsync();

        var headersDict = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(
                pair => pair.Key,
                pair => string.Join(", ", pair.Value)
            );

        return new HttpResponseBytes(
            (int) response.StatusCode,
            headersDict,
            responseBody
        );
    }

    public async ValueTask<HttpResponseString> RequestHttpString(
        string url,
        string method,
        IDictionary<string, object>? headers,
        string? body
    ) {
        EnsureUrlIsAllowed(url);

        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (headers != null) {
            foreach (var header in headers) {
                request.Headers.Add(header.Key, header.Value.ToString());
            }
        }

        if (body != null) {
            request.Content = new StringContent(body, Encoding.UTF8);
        }

        var response = await _client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        var headersDict = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(
                pair => pair.Key,
                pair => string.Join(", ", pair.Value)
            );

        return new HttpResponseString(
            (int)response.StatusCode,
            headersDict,
            responseBody
        );
    }
    
    private void EnsureUrlIsAllowed(string url) {
        var uri = new Uri(url);
        var mode = _configuration.Network.Mode;
        
        if (mode == NetworkMode.AllowAll) {
            return;
        }
        
        if (mode == NetworkMode.BlockAll) {
            throw new InvalidOperationException("Network is blocked");
        }
        
        if (mode == NetworkMode.AllowSome) {
            var addresses = _configuration.Network.AllowedAddresses;
            if (!addresses.Contains(uri.Host)) {
                throw new InvalidOperationException("Network is blocked");
            }
        }
        
        if (mode == NetworkMode.BlockSome) {
            var addresses = _configuration.Network.BlockedAddresses;
            if (addresses.Contains(uri.Host)) {
                throw new InvalidOperationException("Network is blocked");
            }
        }
        
        throw new InvalidOperationException("Invalid network mode");
    }
}
