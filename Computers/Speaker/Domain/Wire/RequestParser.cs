using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;

namespace Computers.Speaker.Domain.Wire;

public static class SpeakerRequestParser {
    public static SpeakerRequest ParseBody(JObject payload) {
        var cmd = payload["cmd"]?.Value<string>();
        switch (cmd) {
            case "ping":
                return new SpeakerPingRequest();

            case "play": {
                var cue = payload["cue"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(cue)) {
                    throw new PeripheralRequestException("cue is required");
                }

                int? pitch = null;
                
                if (payload["pitch"] is not { } token || token.Type == JTokenType.Null) {
                    return new PlayRequest(cue, pitch);
                }
                
                if (token.Type != JTokenType.Integer) {
                    throw new PeripheralRequestException("pitch must be an integer");
                }
                
                pitch = token.Value<int>();
                return new PlayRequest(cue, pitch);
            }

            default:
                throw new PeripheralRequestException($"unknown command '{cmd}'");
        }
    }
}
