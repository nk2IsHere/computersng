using System.Text;
using Computers.Game;

namespace Computers.Computer.Domain;

public class StorageComputerApi: IComputerApi {
    public string Name => "Storage";
    public bool ShouldExpose => true;
    public object Api => _state;
    
    public ISet<Type> ReceivableEvents => new HashSet<Type>();
    public IRedundantLoader LibraryLoader => new StorageRedundantLoader(_state);
    
    private readonly StorageComputerState _state;

    public StorageComputerApi(IComputerPort computerPort, IEnumerable<IStorageLayer> layers) {
        _state = new StorageComputerState(layers);
    }
    
    public void ReceiveEvent(IComputerEvent computerEvent) {
    }

    public void Reset() {
    }
}

internal class StorageComputerState: IStorageLayer {
    private readonly List<IStorageLayer> _layers;

    public StorageComputerState(IEnumerable<IStorageLayer> layers) {
        _layers = layers
            .OrderBy(layer => layer.Priority)
            .ToList();
    }

    public int Priority => 0;
    public StorageLayerMode Mode => StorageLayerMode.ReadWrite;

    public bool Exists(string path) {
        return _layers.Any(layer => layer.Exists(path));
    }

    public StorageResponse MakeDirectory(string path) {
        if (Exists(path)) {
            return StorageResponse.OfError(StorageErrorType.DirectoryAlreadyExists);
        }

        var highestPriorityReadWriteLayer = _layers.First(layer => layer.Mode == StorageLayerMode.ReadWrite);
        return highestPriorityReadWriteLayer.MakeDirectory(path);
    }

    public StorageResponse Delete(string path, bool recursive = false) {
        var layersWhereEntryExists = _layers
            .Where(layer => layer.Exists(path))
            .ToList();

        if (layersWhereEntryExists.Count == 0) {
            return StorageResponse.OfError(StorageErrorType.FileNotFound);
        }

        if (layersWhereEntryExists.All(layer => layer.Mode == StorageLayerMode.ReadOnly)) {
            return StorageResponse.OfError(StorageErrorType.ReadOnlyLayer);
        }

        if (layersWhereEntryExists.All(layer => layer.Mode == StorageLayerMode.ReadWrite)) {
            return layersWhereEntryExists.First().Delete(path, recursive);
        }

        var highestPriorityReadWriteLayer = layersWhereEntryExists.First(layer => layer.Mode == StorageLayerMode.ReadWrite);
        return highestPriorityReadWriteLayer.Delete(path, recursive);
    }

    public StorageResponse<StorageFile> Read(string path) {
        var layersWhereEntryExists = _layers
            .Where(layer => layer.Exists(path))
            .ToList();

        if (layersWhereEntryExists.Count == 0) {
            return StorageResponse<StorageFile>.OfError(StorageErrorType.FileNotFound);
        }

        var highestPriorityLayer = layersWhereEntryExists.First();
        return highestPriorityLayer.Read(path);
    }

    public StorageResponse<StorageFileMetadata> ReadMetadata(string path) {
        var layersWhereEntryExists = _layers
            .Where(layer => layer.Exists(path))
            .ToList();

        if (layersWhereEntryExists.Count == 0) {
            return StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.FileNotFound);
        }

        var highestPriorityLayer = layersWhereEntryExists.First();
        return highestPriorityLayer.ReadMetadata(path);
    }

    public StorageResponse<StorageFileMetadata[]> List(string path) {
        var layers = _layers.Select(layer => layer.List(path)).ToList();
        
        var errorsCount = layers.Count(layer => layer.Type == StorageResponseType.Error);
        if (errorsCount == layers.Count) {
            return StorageResponse<StorageFileMetadata[]>.OfError(StorageErrorType.DirectoryNotFound);
        }
        
        var metadata = layers
            .Where(response => response is { Type: StorageResponseType.Success, Data: not null })
            .SelectMany(response => response.Data!)
            .ToArray();
       
        return StorageResponse<StorageFileMetadata[]>.OfSuccess(metadata);
    }

    public StorageResponse Write(string path, byte[] value) {
        if (Exists(path)) {
            return StorageResponse.OfError(StorageErrorType.FileAlreadyExists);
        }
        
        var highestPriorityReadWriteLayer = _layers.First(layer => layer.Mode == StorageLayerMode.ReadWrite);
        return highestPriorityReadWriteLayer.Write(path, value);
    }
}

internal class StorageRedundantLoader : IRedundantLoader {
    
    private readonly StorageComputerState _state;

    public StorageRedundantLoader(StorageComputerState state) {
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

    public string[] List(string path) {
        var response = _state.List(path);
        if (response is not { Type: StorageResponseType.Success, Data: not null }) {
            throw new InvalidOperationException($"Failed to list files in {path}: {response.Error}");
        }

        return response.Data
            .Select(metadata => metadata.Name)
            .ToArray();
    }

    public bool Exists(string path) {
        return _state.Exists(path);
    }
}
