using Computers.Core;
using StardewModdingAPI;
using StardewValley.GameData.Machines;

namespace Computers.Game.Domain;

public class MachinePatcherService: IPatcherService {

    private readonly IMonitor _monitor;
    private readonly ISet<ContextEntry<Machine>> _machines;

    public MachinePatcherService(IMonitor monitor, ISet<ContextEntry<Machine>> machines) {
        _monitor = monitor;
        _machines = machines;
        
        monitor.Log($"MachinePatcherService initialized with {machines.Count} machines.", LogLevel.Debug);
    }

    public bool CanPatch(Type assetType, IAssetName assetName) {
        return assetType == typeof(Dictionary<string, MachineData>) 
               && assetName.IsEquivalentTo("Data/Machines");
    }

    public void Patch(IAssetData asset) {
        _monitor.Log("Patching machine data.", LogLevel.Debug);
        var assetData = asset.GetData<Dictionary<string, MachineData>>();
        
        foreach (var machine in _machines) {
            assetData.Add(machine.Value.ConnectionId, machine.Value.Data);
        }
    }
}
