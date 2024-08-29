using StardewModdingAPI;

namespace Computers.Game.Boundary;

public interface ILoader {
    public object Load(IAssetName assetName);
}

public interface ILoaderService : ILoader {
    public bool ShouldLoad(Type assetType, IAssetName assetName);
}

public interface ITargetLoader<T> {
    public T Load();
}
