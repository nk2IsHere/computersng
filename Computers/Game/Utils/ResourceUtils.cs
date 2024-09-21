using StardewModdingAPI;

namespace Computers.Game.Utils;

public static class ResourceUtils {
    public static string[] PathSplitters { get; } = { "/", "\\" };
    
    public static string[] SplitPath(string path) {
        return path.Split(PathSplitters, StringSplitOptions.RemoveEmptyEntries);
    }
    
    public static string ResolvePath(string basePath, string path) {
        var basePathParts = SplitPath(basePath);
        var pathParts = SplitPath(path);
        return Path.Combine(basePathParts.Concat(pathParts).ToArray());
    }
    
    public static string ResolvePath(IEnumerable<string> basePathParts, string path) {
        var pathParts = SplitPath(path);
        return Path.Combine(basePathParts.Concat(pathParts).ToArray());
    }
    
    public static string ResolvePath(this IModHelper helper, string basePath, string path) {
        var helperDirectory = helper.DirectoryPath;
        var basePathParts = SplitPath(basePath);
        var pathParts = SplitPath(path);
        return Path.Combine(helperDirectory, Path.Combine(basePathParts.Concat(pathParts).ToArray()));
    }
    
    public static string ResolvePath(this IModHelper helper, IEnumerable<string> basePathParts, string path) {
        var helperDirectory = helper.DirectoryPath;
        var pathParts = SplitPath(path);
        return Path.Combine(helperDirectory, Path.Combine(basePathParts.Concat(pathParts).ToArray()));
    }
    
    public static string ResolvePath(this IModHelper helper, string path) {
        var helperDirectory = helper.DirectoryPath;
        var pathParts = SplitPath(path);
        return Path.Combine(helperDirectory, Path.Combine(pathParts));
    }
    
    public static string LoadString(this IModHelper helper, string basePath, string path) {
        var fullPath = ResolvePath(helper, basePath, path);
        
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException($"Asset not found: {fullPath}");
        }
        
        return File.ReadAllText(fullPath);
    }
    
    public static string LoadString(this IModHelper helper, IEnumerable<string> basePathParts, string path) {
        var fullPath = ResolvePath(helper, basePathParts, path);
        
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException($"Asset not found: {fullPath}");
        }
        
        return File.ReadAllText(fullPath);
    }

    public static string LoadString(this IModHelper helper, string path) {
        var fullPath = ResolvePath(helper, path);
        
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException($"Asset not found: {fullPath}");
        }
        
        return File.ReadAllText(fullPath);
    }
}
