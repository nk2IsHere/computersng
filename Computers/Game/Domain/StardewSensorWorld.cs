using Computers.PlayerSensor;
using Computers.PlayerSensor.Domain.Wire;
using Computers.Router.Domain;
using StardewValley;

namespace Computers.Game.Domain;

public class StardewSensorWorld : ISensorWorld {
    public SensorReading? ReadingFor(Placement placement, int radius) {
        var location = Game1.getLocationFromName(placement.Location);
        if (location is null) {
            return null;
        }

        var players = location.farmers
            .Select(farmer => new SensedCharacter("player", farmer.Name, farmer.TilePoint.X, farmer.TilePoint.Y))
            .Where(character => InRadius(placement, character, radius))
            .ToList();

        var npcs = location.characters
            .Select(npc => new SensedCharacter("npc", npc.Name, npc.TilePoint.X, npc.TilePoint.Y))
            .Where(character => InRadius(placement, character, radius))
            .ToList();

        return new SensorReading(players, npcs);
    }

    private static bool InRadius(Placement placement, SensedCharacter character, int radius) {
        var dx = character.X - placement.X;
        var dy = character.Y - placement.Y;
        return dx * dx + dy * dy <= radius * radius;
    }
}
