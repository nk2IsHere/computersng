using Computers.Game.Boundary;
using Computers.Game.Utils;
using StardewModdingAPI;

namespace Computers.Game;

public class RedundantLoader: IRedundantLoader {
    
    private readonly IModHelper _helper;
    private readonly string[] _basePath;
    
    public RedundantLoader(IModHelper helper, string basePath) {
        _helper = helper;
        _basePath = basePath.Split(ResourceUtils.PathSplitters, StringSplitOptions.RemoveEmptyEntries);
    }

    public T Load<T>(string path) where T : notnull {
        var assetNameParts = path.Split(ResourceUtils.PathSplitters, StringSplitOptions.RemoveEmptyEntries);
        var assetPath = Path.Combine(Path.Combine(_basePath), Path.Combine(assetNameParts));
        
        try {
            return _helper.ModContent.Load<T>(assetPath);
        } catch (Exception e) {
            // Sometimes we want to load non-standard assets, like a .lua file
            // In that case, we can use the following code to load the asset
            // from the mod's directory
            
            if (!typeof(T).IsAssignableFrom(typeof(string))) {
                throw;
            }
            
            return (T)(object)_helper.LoadString(assetPath);
        }
    }

    public bool Exists(string path) {
        var directory = _helper.DirectoryPath;
        var assetNameParts = path.Split(ResourceUtils.PathSplitters, StringSplitOptions.RemoveEmptyEntries);
        var assetPath = Path.Combine(Path.Combine(_basePath), Path.Combine(assetNameParts));
        var fullPath = Path.Combine(directory, assetPath);
        
        return File.Exists(fullPath);
    }
}
