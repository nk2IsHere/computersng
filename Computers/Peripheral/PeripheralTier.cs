namespace Computers.Peripheral;

public enum PeripheralTier {
    Basic,
    Advanced
}

public record TieredPingResult(string Type, PeripheralTier Tier);
