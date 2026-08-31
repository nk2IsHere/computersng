using Computers.Peripheral;
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

public record SensorPingResult(string Type, PeripheralTier Tier, int Radius);

public abstract record SensorRequest;

public record SensorPingRequest : SensorRequest;

public record SensorReadRequest : SensorRequest;

public record SensorConfigureRequest(int Radius) : SensorRequest;
