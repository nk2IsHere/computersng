using Computers.Core;
using Computers.Game.Boundary;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace Computers.Game;

public record TileSheet(
    string AssetId,
    string TileSheetPath
);

public class TileSheetLoader : ILoaderService {
    private readonly IMonitor _monitor;
    private readonly IModHelper _helper;
    private readonly ISet<ContextEntry<TileSheet>> _tileSheets;

    public TileSheetLoader(
        IMonitor monitor,
        IModHelper helper,
        ISet<ContextEntry<TileSheet>> tileSheets
    ) {
        _monitor = monitor;
        _helper = helper;
        _tileSheets = tileSheets;

        monitor.Log($"TileSheetLoader initialized with {tileSheets.Count} tile sheets.", LogLevel.Debug);
    }

    public bool ShouldLoad(Type assetType, IAssetName assetName) {
        return assetType == typeof(Texture2D)
               && _tileSheets.Any(tileSheet => assetName.IsEquivalentTo(tileSheet.Value.AssetId));
    }

    public object Load(IAssetName assetName) {
        _monitor.Log($"Loading tile sheet: {assetName.Name}", LogLevel.Debug);
        var tileSheet = _tileSheets.First(tileSheet => assetName.IsEquivalentTo(tileSheet.Value.AssetId));
        return _helper.ModContent.Load<Texture2D>(tileSheet.Value.TileSheetPath);
    }
}