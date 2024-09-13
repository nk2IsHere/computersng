using System.Text;
using Computers.Computer.Domain.Storage;
using Computers.Game;

namespace Computers.Computer.Domain.Api;

public class StorageComputerApi: IComputerApi {
    public string Name => "Storage";
    public bool ShouldExpose => true;
    public object Api => _state;
    
    public ISet<Type> ReceivableEvents => new HashSet<Type>();
    public IRedundantLoader LibraryLoader => new StorageRedundantLoader(_state);
    
    private readonly IStorageLayer _state;

    public StorageComputerApi(IComputerPort computerPort, Func<StorageComputerApi, IEnumerable<IStorageLayer>> layers) {
        _state = new CompositeStorageLayer(layers(this));
    }
    
    public void ReceiveEvent(IComputerEvent computerEvent) {
    }

    public void Reset() {
    }
}

internal class StorageRedundantLoader : IRedundantLoader {
    
    private readonly IStorageLayer _state;

    public StorageRedundantLoader(IStorageLayer state) {
        _state = state;
    }
    
    public T Load<T>(string path) where T : notnull {
        var response = _state.Read(path);
        if (response.Type == StorageResponseType.Error) {
            throw new InvalidOperationException($"Failed to load {typeof(T).Name} from {path}: {response.Error}");
        }
        
        if (response.Data is not { } file) {
            throw new InvalidOperationException($"Failed to load {typeof(T).Name} from {path}: Expected file, got {response.Data?.GetType().Name}");
        }

        return typeof(T) switch {
            { } t when t == typeof(byte[]) => (T) (object) file.Data,
            { } t when t == typeof(string) => (T) (object) Encoding.UTF8.GetString(file.Data),
            _ => throw new InvalidOperationException($"Failed to load {typeof(T).Name} from {path}: Unsupported type")
        };
    }

    public IEnumerable<FileSystemEntry> List(string path) {
        var response = _state.List(path);
        if (response is not { Type: StorageResponseType.Success, Data: not null }) {
            throw new InvalidOperationException($"Failed to list files in {path}: {response.Error}");
        }

        return response.Data
            .Select(metadata => new FileSystemEntry(
                metadata.Name,
                metadata.Type switch {
                    StorageFileType.File => FileSystemEntryType.File,
                    StorageFileType.Directory => FileSystemEntryType.Directory,
                    _ => throw new ArgumentOutOfRangeException()
                },
                metadata.Size
            ))
            .ToArray();
    }

    public bool Exists(string path) {
        return _state.Exists(path);
    }
}
