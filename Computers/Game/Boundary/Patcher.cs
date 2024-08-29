using StardewModdingAPI;

namespace Computers.Game.Boundary;

public interface IPatcherService {
    public bool CanPatch(Type assetType, IAssetName assetName);
    public void Patch(IAssetData asset);
}
