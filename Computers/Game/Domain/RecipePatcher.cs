using Computers.Core;
using StardewModdingAPI;

namespace Computers.Game.Domain;

public class RecipePatcherService : IPatcherService {
    private readonly IMonitor _monitor;
    private readonly ISet<ContextEntry<Recipe>> _patchData;

    public RecipePatcherService(
        IMonitor monitor,
        ISet<ContextEntry<Recipe>> patchData
    ) {
        _monitor = monitor;
        _patchData = patchData;
    }

    public bool CanPatch(Type assetType, IAssetName assetName) {
        return assetType == typeof(Dictionary<string, string>)
            && assetName.IsEquivalentTo("Data/CraftingRecipes");
    }

    public void Patch(IAssetData asset) {
        _monitor.Log("Patching crafting recipes data.", LogLevel.Debug);
        var assetData = asset.GetData<Dictionary<string, string>>();
        foreach (var data in _patchData) {
            assetData.Add(data.Value.PatchKey, data.Value.PatchString);
        }
    }
}
