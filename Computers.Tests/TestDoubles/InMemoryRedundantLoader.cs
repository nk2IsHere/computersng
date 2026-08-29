using Computers.Game;

namespace Computers.Tests.TestDoubles;

public class InMemoryRedundantLoader : IRedundantLoader {
    private readonly Dictionary<string, string> _files;

    public InMemoryRedundantLoader(Dictionary<string, string> files) {
        _files = files;
    }

    public T Load<T>(string path) where T : notnull {
        if (!_files.TryGetValue(path, out var content)) {
            throw new FileNotFoundException(path);
        }

        if (typeof(T) != typeof(string)) {
            throw new NotSupportedException($"InMemoryRedundantLoader only serves strings, requested {typeof(T)}");
        }

        return (T) (object) content;
    }

    public IEnumerable<FileSystemEntry> List(string path) {
        var prefix = path.TrimEnd('/') + "/";
        var names = _files.Keys
            .Where(file => file.StartsWith(prefix))
            .Select(file => file[prefix.Length..])
            .Where(rest => !rest.Contains('/'))
            .ToList();

        if (names.Count == 0) {
            throw new DirectoryNotFoundException(path);
        }

        return names.Select(name => new FileSystemEntry(
            name,
            FileSystemEntryType.File,
            _files[prefix + name].Length
        ));
    }

    public bool Exists(string path) {
        return _files.ContainsKey(path) || _files.Keys.Any(file => file.StartsWith(path.TrimEnd('/') + "/"));
    }
}
