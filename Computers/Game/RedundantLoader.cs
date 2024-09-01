using Computers.Game.Boundary;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI;

namespace Computers.Game;

public class RedundantLoader: IRedundantLoader {
    
    private readonly IModHelper _helper;
    private readonly string[] _basePath;
    
    public RedundantLoader(IModHelper helper, string basePath) {
        _helper = helper;
        _basePath = basePath.Split(ILoader.PathSplitters, StringSplitOptions.RemoveEmptyEntries);
    }

    public T Load<T>(string path) where T : notnull {
        try {
            var assetNameParts = path.Split(ILoader.PathSplitters, StringSplitOptions.RemoveEmptyEntries);
            var assetPath = Path.Combine(Path.Combine(_basePath), Path.Combine(assetNameParts));
            return _helper.ModContent.Load<T>(assetPath);
        } catch (Exception e) {
            // Sometimes we want to load non-standard assets, like a .lua file
            // In that case, we can use the following code to load the asset
            // from the mod's directory
            
            if (!typeof(T).IsAssignableFrom(typeof(string))) {
                throw;
            }
            
            var directory = _helper.DirectoryPath;
            var assetNameParts = path.Split(ILoader.PathSplitters, StringSplitOptions.RemoveEmptyEntries);
            var assetPath = Path.Combine(Path.Combine(_basePath), Path.Combine(assetNameParts));
            var fullPath = Path.Combine(directory, assetPath);
            
            if (!File.Exists(fullPath)) {
                throw new FileNotFoundException($"Asset not found: {fullPath}", e);
            }
            
            return (T) (object) File.ReadAllText(fullPath);
        }
    }
}
