using StardewModdingAPI;

namespace Computers.Game.Utils;

public static class ResourceUtils {
    public static string[] PathSplitters { get; } = { "/", "\\" };
    
    public static string[] SplitPath(string path) {
        return path.Split(PathSplitters, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string LoadString(this IModHelper helper, string path) {
        var directory = helper.DirectoryPath;
        var pathParts = path.Split(PathSplitters, StringSplitOptions.RemoveEmptyEntries);
        var fullPath = Path.Combine(directory, Path.Combine(pathParts));
        
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException($"Asset not found: {fullPath}");
        }
        
        return File.ReadAllText(fullPath);
    }
}
