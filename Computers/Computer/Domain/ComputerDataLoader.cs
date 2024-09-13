using Computers.Game;
using Computers.Game.Utils;

namespace Computers.Computer.Domain;

internal class ComputerDataLoader : IRedundantLoader {
    private readonly IComputerPort _computerPort;
    private readonly IRedundantLoader _dataLoader;

    public ComputerDataLoader(IComputerPort computerPort, IRedundantLoader dataLoader) {
        _computerPort = computerPort;
        _dataLoader = dataLoader;
    }

    public T Load<T>(string path) where T : notnull {
        var pathParts = ResourceUtils.SplitPath(path);
        return _dataLoader.Load<T>(Path.Combine(_computerPort.Id, Path.Combine(pathParts)));
    }

    public IEnumerable<FileSystemEntry> List(string path) {
        var pathParts = ResourceUtils.SplitPath(path);
        return _dataLoader.List(Path.Combine(_computerPort.Id, Path.Combine(pathParts)));
    }

    public bool Exists(string path) {
        var pathParts = ResourceUtils.SplitPath(path);
        return _dataLoader.Exists(Path.Combine(_computerPort.Id, Path.Combine(pathParts)));
    }
}
