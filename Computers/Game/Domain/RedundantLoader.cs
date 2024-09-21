using Computers.Game.Utils;
using StardewModdingAPI;

namespace Computers.Game.Domain;

public class RedundantLoader: IRedundantLoader {
    
    private readonly IModHelper _helper;
    private readonly string[] _basePath;
    
    public RedundantLoader(IModHelper helper, string basePath) {
        _helper = helper;
        _basePath = basePath.Split(ResourceUtils.PathSplitters, StringSplitOptions.RemoveEmptyEntries);
    }

    public T Load<T>(string path) where T : notnull {
        var baseResolvedPath = ResourceUtils.ResolvePath(_basePath, path);
        
        try {
            return _helper.ModContent.Load<T>(baseResolvedPath);
        } catch (Exception) {
            // Sometimes we want to load non-standard assets, like a .lua file
            // In that case, we can use the following code to load the asset
            // from the mod's directory
            
            if (!typeof(T).IsAssignableFrom(typeof(string))) {
                throw;
            }
            
            return (T)(object)_helper.LoadString(baseResolvedPath);
        }
    }

    public IEnumerable<FileSystemEntry> List(string path) {
        var fullPath = _helper.ResolvePath(_basePath, path);

        var files = Directory
            .GetFiles(fullPath)
            .Select(file => new FileInfo(file));
        
        var fileEntries = files
            .Select(file => new FileSystemEntry(
                file.Name, 
                FileSystemEntryType.File, 
                file.Length
            ));
        
        var directories = Directory
            .GetDirectories(fullPath)
            .Select(directory => new DirectoryInfo(directory));
        
        var directoryEntries = directories
            .Select(directory => new FileSystemEntry(
                directory.Name,
                FileSystemEntryType.Directory, 
                0
            ));
        
        return fileEntries.Concat(directoryEntries);
    }

    public bool Exists(string path) {
        var fullPath = _helper.ResolvePath(_basePath, path);
        return File.Exists(fullPath) || Directory.Exists(fullPath);
    }
}
