using Computers.Game.Utils;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI;

namespace Computers.Game.Domain;

public class TargetLoader<T> : ITargetLoader<T> where T : notnull {
    
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
            
            return (T)(object)_helper.LoadString(_assetName);
        }
    }
}
