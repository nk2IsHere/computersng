using Computers.Core;
using Computers.Game.Boundary;
using StardewModdingAPI;
using StardewValley.GameData.BigCraftables;

namespace Computers.Game;

public class BigCraftablePatcherService : IPatcherService {
    private readonly IMonitor _monitor;
    private readonly ISet<ContextEntry<BigCraftableData>> _patchData;

    public BigCraftablePatcherService(
        IMonitor monitor,
        ISet<ContextEntry<BigCraftableData>> patchData
    ) {
        _monitor = monitor;
        _patchData = patchData;

        monitor.Log($"BigCraftablePatcherService initialized with {patchData.Count} patch data.", LogLevel.Debug);
    }

    public bool CanPatch(Type assetType, IAssetName assetName) {
        return assetType == typeof(Dictionary<string, BigCraftableData>) 
               && assetName.IsEquivalentTo("Data/BigCraftables");
    }

    public void Patch(IAssetData asset) {
        _monitor.Log("Patching BigCraftables data.", LogLevel.Debug);
        var assetData = asset.GetData<Dictionary<string, BigCraftableData>>();
        foreach (var data in _patchData) {
            assetData.Add(data.Id.Value, data.Value);
        }
    }
}
