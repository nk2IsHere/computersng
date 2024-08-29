using StardewValley.GameData.Machines;

namespace Computers.Game.Boundary;

public record Machine(
    string ConnectionId,
    MachineData Data
);
