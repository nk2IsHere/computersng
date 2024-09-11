using StardewValley.GameData.Machines;

namespace Computers.Game;

public record Machine(
    string ConnectionId,
    MachineData Data
);
