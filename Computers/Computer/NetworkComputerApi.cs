using Computers.Computer.Boundary;
using Computers.Game.Boundary;

namespace Computers.Computer;

public class NetworkComputerApi : IComputerApi {
    public string Name => "Network";
    public bool ShouldExpose => true;
    public object Api => _state;
    
    public ISet<Type> ReceivableEvents => new HashSet<Type>();
    public IRedundantLoader? LibraryLoader => null;

    private readonly NetworkComputerState _state;

    public NetworkComputerApi(IComputerPort computerPort) {
        _state = new NetworkComputerState(computerPort.Configuration);
    }

    public void ReceiveEvent(IComputerEvent computerEvent) {
    }

    public void Reset() {
    }
}

internal class NetworkComputerState {
    
    private readonly Configuration _configuration;
    private readonly HttpClient _client = new();

    public NetworkComputerState(Configuration configuration) {
        _configuration = configuration;
    }

    public async Task<byte[]> RequestHttp(
        string url,
        string method,
        IDictionary<string, string>? headers,
        byte[]? body
    ) {
        EnsureUrlIsAllowed(url);
        
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        
        if (headers != null) {
            foreach (var header in headers) {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        
        if (body != null) {
            request.Content = new ByteArrayContent(body);
        }
        
        var response = await _client.SendAsync(request);
        return await response.Content.ReadAsByteArrayAsync();
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
