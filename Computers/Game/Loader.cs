using StardewModdingAPI;

namespace Computers.Game;

public interface ILoader {
    public object Load(IAssetName assetName);
}

public interface ILoaderService : ILoader {
    public bool ShouldLoad(Type assetType, IAssetName assetName);
}

public interface ITargetLoader<T> {
    public T Load();
}

public enum FileSystemEntryType {
    File,
    Directory
}

public record FileSystemEntry(string Name, FileSystemEntryType Type, long Size);

public interface IRedundantLoader {
    public T Load<T>(string path) where T : notnull;
    public IEnumerable<FileSystemEntry> List(string path);
    public bool Exists(string path);
}
