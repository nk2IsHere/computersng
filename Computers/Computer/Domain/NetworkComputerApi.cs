using System.Text;
using Computers.Game;

namespace Computers.Computer.Domain;

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
    private readonly HttpClient _client = new();

    public NetworkComputerState(Configuration configuration) {
        _configuration = configuration;
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
