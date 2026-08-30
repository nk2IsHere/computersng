using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;

namespace Computers.PlayerSensor.Domain.Wire;

public record SensedCharacter(string Kind, string Name, int X, int Y);

public record SensorReading(
    IReadOnlyList<SensedCharacter> Players,
    IReadOnlyList<SensedCharacter> Npcs
);

public record PresenceEvent(
    string Event,
    IReadOnlyList<SensedCharacter> Entered,
    IReadOnlyList<SensedCharacter> Left
);

public record SensorPingResult(string Type, int Radius);

public abstract record SensorRequest;

public record SensorPingRequest : SensorRequest;

public record SensorReadRequest : SensorRequest;

public record SensorConfigureRequest(int Radius) : SensorRequest;

public static class SensorRequestParser {
    public const int MaxRadius = 64;

    public static SensorRequest ParseBody(JObject payload) {
        var cmd = payload["cmd"]?.Value<string>();
        switch (cmd) {
            case "ping":
                return new SensorPingRequest();

            case "read":
                return new SensorReadRequest();

            case "configure": {
                var token = payload["radius"];
                if (token is null || token.Type != JTokenType.Integer) {
                    throw new PeripheralRequestException("configure requires an integer radius");
                }
                var radius = token.Value<int>();
                return radius is < 1 or > MaxRadius 
                    ? throw new PeripheralRequestException($"radius must be between 1 and {MaxRadius}") 
                    : new SensorConfigureRequest(radius);
            }

            default:
                throw new PeripheralRequestException($"unknown command '{cmd}'");
        }
    }
}
