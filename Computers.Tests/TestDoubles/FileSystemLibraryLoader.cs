using Computers.Game;

namespace Computers.Tests.TestDoubles;

/// <summary>
/// IRedundantLoader over the repo's real assets/Library folder, so Jint-driven tests
/// import the actual shipped modules rather than copies.
/// </summary>
public class FileSystemLibraryLoader : IRedundantLoader {
    private readonly string _root;

    public FileSystemLibraryLoader() {
        _root = FindLibraryRoot();
    }

    public static string FindLibraryRoot() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null) {
            var candidate = Path.Combine(directory.FullName, "Computers", "assets", "Library");
            if (Directory.Exists(candidate)) {
                return candidate;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Computers/assets/Library not found above " + AppContext.BaseDirectory);
    }

    public T Load<T>(string path) where T : notnull {
        if (typeof(T) != typeof(string)) {
            throw new NotSupportedException($"FileSystemLibraryLoader only serves strings, requested {typeof(T)}");
        }

        return (T) (object) File.ReadAllText(Resolve(path));
    }

    public IEnumerable<FileSystemEntry> List(string path) {
        var full = Resolve(path);
        foreach (var entry in Directory.EnumerateFileSystemEntries(full)) {
            var name = Path.GetFileName(entry);
            if (Directory.Exists(entry)) {
                yield return new FileSystemEntry(name, FileSystemEntryType.Directory, 0);
            }
            else {
                yield return new FileSystemEntry(name, FileSystemEntryType.File, new FileInfo(entry).Length);
            }
        }
    }

    public bool Exists(string path) {
        var full = Resolve(path);
        return File.Exists(full) || Directory.Exists(full);
    }

    private string Resolve(string path) {
        var parts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        return Path.Combine(_root, Path.Combine(parts));
    }
}
