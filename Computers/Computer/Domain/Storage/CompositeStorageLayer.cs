namespace Computers.Computer.Domain.Storage;


public class CompositeStorageLayer: IStorageLayer {
    private readonly List<IStorageLayer> _layers;

    public CompositeStorageLayer(IEnumerable<IStorageLayer> layers) {
        _layers = layers
            .OrderBy(layer => layer.Priority)
            .ToList();
        
        // Only one Read Write layer is allowed
        var readWriteLayersCount = _layers.Count(layer => layer.Mode == StorageLayerMode.ReadWrite);
        if (readWriteLayersCount > 1) {
            throw new InvalidOperationException("Only one ReadWrite layer is allowed");
        }
        
        // Read Write layer must be the last one
        if (_layers.Last().Mode != StorageLayerMode.ReadWrite) {
            throw new InvalidOperationException("ReadWrite layer must be the last one");
        }
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
            .DistinctBy(response => response.Name)
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
