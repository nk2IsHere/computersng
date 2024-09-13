namespace Computers.Computer.Domain.Storage;

internal class PersistentStorageLayer: IStorageLayer {
    
    private static string LayerName => "Persistent";
    
    private readonly Dictionary<string, object> _storage;
    
    public PersistentStorageLayer(IDictionary<string, object> storage, int priority = int.MaxValue) {
        _storage = new Dictionary<string, object>(storage);
        Priority = priority;
    }
    
    public int Priority { get; }
    public StorageLayerMode Mode => StorageLayerMode.ReadWrite;
    
    public bool Exists(string path) {
        var pathParts = IStorageLayer.CleanPath(path);
        return ExistsRecursive(pathParts, _storage);
    }
    
    public StorageResponse MakeDirectory(string path) {
        var pathParts = IStorageLayer.CleanPath(path);
        return MakeDirectoryRecursive(pathParts, _storage);
    }
    
    public StorageResponse Delete(string path, bool recursive = false) {
        var pathParts = IStorageLayer.CleanPath(path);
        return DeleteRecursive(pathParts, recursive, _storage);
    }
    
    public StorageResponse<StorageFile> Read(string path) {
        var pathParts = IStorageLayer.CleanPath(path);
        return ReadRecursive(pathParts, _storage);
    }
    
    public StorageResponse<StorageFileMetadata> ReadMetadata(string path) {
        var pathParts = IStorageLayer.CleanPath(path);
        return ReadMetadataRecursive(pathParts, _storage);
    }
    
    public StorageResponse<StorageFileMetadata[]> List(string path) {
        var pathParts = IStorageLayer.CleanPath(path);
        return ListRecursive(pathParts, _storage);
    }
    
    public StorageResponse Write(string path, byte[] value) {
        var pathParts = IStorageLayer.CleanPath(path);
        return WriteRecursive(pathParts, _storage, value);
    }
    
    private static StorageResponse<StorageFileMetadata[]> ListRecursive(string[] pathParts, IDictionary<string, object> storage) {
        while (true) {
            if (pathParts.Length == 0) {
                var metadataList = storage
                    .Select(entry => {
                        var (name, value) = entry;
                        return value switch {
                            byte[] data => StorageFileMetadata.Of(name, StorageFileType.File, data.Length, LayerName),
                            IDictionary<string, object> _ => StorageFileMetadata.Of(name, StorageFileType.Directory, 0, LayerName),
                            _ => throw new ArgumentOutOfRangeException($"Unknown storage type {value.GetType()}")
                        };
                    })
                    .ToArray();
                
                return StorageResponse<StorageFileMetadata[]>.OfSuccess(metadataList);
            }
            
            var currentPart = pathParts[0];
            if (!storage.ContainsKey(currentPart)) {
                return StorageResponse<StorageFileMetadata[]>.OfError(StorageErrorType.DirectoryNotFound);
            }
            
            if(storage[currentPart] is not IDictionary<string, object> directory) {
                return StorageResponse<StorageFileMetadata[]>.OfError(StorageErrorType.PathIsNotDirectory);
            }
            
            pathParts = pathParts[1..];
            storage = directory;
        }
    }
    
    private static StorageResponse<StorageFileMetadata> ReadMetadataRecursive(string[] pathParts, IDictionary<string, object> storage) {
        while (true) {
            if (pathParts.Length == 0) {
                return StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.FileNotFound);
            }
            
            var currentPart = pathParts[0];
            if (!storage.ContainsKey(currentPart)) {
                return StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.FileNotFound);
            }
            
            if (pathParts.Length == 1) {
                return storage[currentPart] switch {
                    IDictionary<string, object> => StorageResponse<StorageFileMetadata>.OfSuccess(StorageFileMetadata.Of(currentPart, StorageFileType.Directory, 0, LayerName)),
                    byte[] data => StorageResponse<StorageFileMetadata>.OfSuccess(StorageFileMetadata.Of(currentPart, StorageFileType.File, data.Length, LayerName)),
                    _ => StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.PathIsNotFile)
                };
            }
            
            if (storage[currentPart] is not IDictionary<string, object> directory) {
                return StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.PathIsNotDirectory);
            }
            
            pathParts = pathParts[1..];
            storage = directory;
        }
    }
    
    private static StorageResponse<StorageFile> ReadRecursive(string[] pathParts, IDictionary<string, object> storage) {
        while (true) {
            if (pathParts.Length == 0) {
                return StorageResponse<StorageFile>.OfError(StorageErrorType.FileNotFound);
            }
            
            var currentPart = pathParts[0];
            if (!storage.ContainsKey(currentPart)) {
                return StorageResponse<StorageFile>.OfError(StorageErrorType.FileNotFound);
            }
            
            if (pathParts.Length == 1) {
                if (storage[currentPart] is not byte[] data) {
                    return StorageResponse<StorageFile>.OfError(StorageErrorType.PathIsNotFile);
                }
                
                var readData = new byte[data.Length];
                data.CopyTo(readData, 0);
                
                return StorageResponse<StorageFile>.OfSuccess(StorageFile.Of(currentPart, readData, LayerName));

            }
            
            if (storage[currentPart] is not IDictionary<string, object> directory) {
                return StorageResponse<StorageFile>.OfError(StorageErrorType.PathIsNotDirectory);
            }
            
            pathParts = pathParts[1..];
            storage = directory;
        }
    }
    
    private static StorageResponse DeleteRecursive(string[] pathParts, bool recursive, IDictionary<string, object> storage) {
        while (true) {
            if (pathParts.Length == 0) {
                return StorageResponse.OfError(StorageErrorType.FileNotFound);
            }
            
            var currentPart = pathParts[0];
            if (!storage.ContainsKey(currentPart)) {
                return StorageResponse.OfError(StorageErrorType.FileNotFound);
            }
            
            if (pathParts.Length == 1) {
                if (
                    storage[currentPart] is IDictionary<string, object> { Count: > 0 } 
                    && !recursive
                ) {
                    return StorageResponse.OfError(StorageErrorType.DirectoryNotEmpty);
                }
                
                storage.Remove(currentPart);
                return StorageResponse.OfSuccess();
            }
            
            if (storage[currentPart] is not IDictionary<string, object> directory) {
                return StorageResponse.OfError(StorageErrorType.PathIsNotDirectory);
            }
            
            pathParts = pathParts[1..];
            storage = directory;
        }
    }
    
    private static StorageResponse WriteRecursive(string[] pathParts, IDictionary<string, object> storage, byte[] value) {
        while (true) {
            if (pathParts.Length == 0) {
                return StorageResponse.OfError(StorageErrorType.FileNotFound);
            }
            
            var currentPart = pathParts[0];
            if (pathParts.Length == 1) {
                if (storage.ContainsKey(currentPart)) {
                    return StorageResponse.OfError(StorageErrorType.FileAlreadyExists);
                }
                
                var writeData = new byte[value.Length];
                value.CopyTo(writeData, 0);
                
                storage[currentPart] = writeData;
                return StorageResponse.OfSuccess();
            }
            
            if (!storage.ContainsKey(currentPart)) {
                storage[currentPart] = new Dictionary<string, object>();
            }
            
            if (storage[currentPart] is not IDictionary<string, object> directory) {
                return StorageResponse.OfError(StorageErrorType.PathIsNotDirectory);
            }
            
            pathParts = pathParts[1..];
            storage = directory;
        }
    }
    
    private static StorageResponse MakeDirectoryRecursive(string[] pathParts, IDictionary<string, object> storage) {
        while (true) {
            if (pathParts.Length == 0) {
                return StorageResponse.OfError(StorageErrorType.DirectoryNotFound);
            }
            
            var currentPart = pathParts[0];
            if (pathParts.Length == 1) {
                if (storage.ContainsKey(currentPart)) {
                    return StorageResponse.OfError(StorageErrorType.DirectoryAlreadyExists);
                }
                
                storage[currentPart] = new Dictionary<string, object>();
                return StorageResponse.OfSuccess();
            }
            
            if (!storage.ContainsKey(currentPart)) {
                storage[currentPart] = new Dictionary<string, object>();
            }
            
            if (storage[currentPart] is not IDictionary<string, object> directory) {
                return StorageResponse.OfError(StorageErrorType.PathIsNotDirectory);
            }
            
            pathParts = pathParts[1..];
            storage = directory;
        }
    }

    private static bool ExistsRecursive(string[] pathParts, IDictionary<string, object> storage) {
        while (true) {
            if (pathParts.Length == 0) {
                return true;
            }
            
            var currentPart = pathParts[0];
            if (!storage.ContainsKey(currentPart)) {
                return false;
            }
            
            if (pathParts.Length == 1) {
                return true;
            }
            
            if (storage[currentPart] is not IDictionary<string, object> directory) {
                return false;
            }
            
            pathParts = pathParts[1..];
            storage = directory;
        }
    }
}
