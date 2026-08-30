using Newtonsoft.Json.Linq;

namespace Computers.Router.Domain.Wire;

public static class RequestParser {
    public static RouterRequest ParseBody(JObject payload) {
        var cmd = payload["cmd"]?.Value<string>();
        switch (cmd) {
            case "ping":
                return new RouterPingRequest();

            case "discover":
                return new RouterDiscoverRequest();

            case "configure": {
                var token = payload["channel"];
                if (token is null) {
                    throw new RouterRequestException("configure requires channel (integer or null)");
                }
                if (token.Type == JTokenType.Null) {
                    return new RouterConfigureRequest(null);
                }
                if (token.Type != JTokenType.Integer) {
                    throw new RouterRequestException("configure requires channel (integer or null)");
                }
                return new RouterConfigureRequest(token.Value<int>());
            }

            default:
                throw new RouterRequestException($"unknown command '{cmd}'");
        }
    }
}
