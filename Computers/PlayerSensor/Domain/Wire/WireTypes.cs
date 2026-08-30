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

public abstract record SensorRequest;

public record SensorPingRequest : SensorRequest;

public record SensorReadRequest : SensorRequest;

public static class SensorRequestParser {
    public static SensorRequest ParseBody(JObject payload) {
        var cmd = payload["cmd"]?.Value<string>();
        switch (cmd) {
            case "ping":
                return new SensorPingRequest();

            case "read":
                return new SensorReadRequest();

            default:
                throw new PeripheralRequestException($"unknown command '{cmd}'");
        }
    }
}
