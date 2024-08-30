using Computers.Game.Boundary;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI;

namespace Computers.Game;

public class TargetLoader<T> : ITargetLoader<T> where T : notnull {
    
    private static readonly string[] PathSplitters = {"/", "\\"};

    private readonly IModHelper _helper;
    private readonly string _assetName;
    
    public TargetLoader(IModHelper helper, string assetName) {
        _helper = helper;
        _assetName = assetName;
    }

    public T Load() {
        try {
            return _helper.ModContent.Load<T>(_assetName);
        } catch (ContentLoadException e) {
            // Sometimes we want to load non-standard assets, like a .lua file
            // In that case, we can use the following code to load the asset
            // from the mod's directory
            if (!typeof(T).IsAssignableFrom(typeof(string))) {
                throw;
            }
            
            var directory = _helper.DirectoryPath;
            var assetNameParts = _assetName.Split(PathSplitters, StringSplitOptions.RemoveEmptyEntries);
            
            var path = Path.Combine(directory, Path.Combine(assetNameParts));
            return File.Exists(path) 
                ? (T) (object) File.ReadAllText(path) 
                : throw e;
        }
    }
}
