using StardewModdingAPI;

namespace Computers.Game;

public interface IPatcherService {
    public bool CanPatch(Type assetType, IAssetName assetName);
    public void Patch(IAssetData asset);
}
