using Newtonsoft.Json.Linq;

namespace Computers.Multiplayer.Domain.Wire;

public static class ChannelRequestParser {
    private static readonly ISet<string> InputKinds = new HashSet<string> { "key", "leftClick", "rightClick", "wheel" };

    public static ChannelRequest Parse(string cmd, JObject payload) {
        return cmd switch {
            "openScreen" => new OpenScreenRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload)),
            "closeScreen" => new CloseScreenRequest(RequireString(payload, "computerId")),
            "screenInput" => ParseScreenInput(payload),
            "initializeComputer" => new InitializeComputerRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload)),
            _ => throw new ChannelRequestException($"unknown command '{cmd}'")
        };
    }

    private static ScreenInputRequest ParseScreenInput(JObject payload) {
        var kind = RequireString(payload, "kind");
        if (!InputKinds.Contains(kind)) {
            throw new ChannelRequestException($"unsupported input kind '{kind}'");
        }
        return new ScreenInputRequest(RequireString(payload, "computerId"), kind, RequireInt(payload, "a"), RequireInt(payload, "b"));
    }

    private static string ReadLocation(JObject payload) {
        var token = payload["location"];
        return token is null || token.Type == JTokenType.Null ? "Farm" : token.Value<string>()!;
    }

    private static string RequireString(JObject payload, string key) {
        var token = payload[key];
        if (token is null || token.Type != JTokenType.String) {
            throw new ChannelRequestException($"'{key}' must be a string");
        }
        return token.Value<string>()!;
    }

    private static int RequireInt(JObject payload, string key) {
        var token = payload[key];
        if (token is null || token.Type != JTokenType.Integer) {
            throw new ChannelRequestException($"'{key}' must be an integer");
        }
        return token.Value<int>();
    }
}
