using Computers.Core;

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
        return RecurseOverStorage(
            pathParts,
            _storage,
            (substorage, entryName) => substorage.ContainsKey(entryName),
            _ => false
        );
    }
    
    public StorageResponse MakeDirectory(string path) {
        var pathParts = IStorageLayer.CleanPath(path);
        return RecurseOverStorage(
            pathParts,
            _storage,
            (substorage, entryName) => {
                if (substorage.ContainsKey(entryName)) {
                    return StorageResponse.OfError(StorageErrorType.DirectoryAlreadyExists);
                }
                
                substorage[entryName] = new Dictionary<string, object>();
                return StorageResponse.OfSuccess();
            },
            preliminaryFailure => preliminaryFailure switch {
                RecurseOverStoragePreliminaryFailure.PathNotFound => StorageResponse.OfError(StorageErrorType.DirectoryNotFound),
                RecurseOverStoragePreliminaryFailure.PathNotDirectory => StorageResponse.OfError(StorageErrorType.PathIsNotDirectory),
                _ => throw new ArgumentOutOfRangeException()
            }
        );
    }
    
    public StorageResponse Delete(string path, bool recursive = false) {
        var pathParts = IStorageLayer.CleanPath(path);
        return RecurseOverStorage(
            pathParts,
            _storage,
            (substorage, entryName) => {
                if (!substorage.ContainsKey(entryName)) {
                    return StorageResponse.OfError(StorageErrorType.FileNotFound);
                }
                
                if (
                    substorage[entryName] is IDictionary<string, object> { Count: > 0 } 
                    && !recursive
                ) {
                    return StorageResponse.OfError(StorageErrorType.DirectoryNotEmpty);
                }
                
                substorage.Remove(entryName);
                return StorageResponse.OfSuccess();
            },
            preliminaryFailure => preliminaryFailure switch {
                RecurseOverStoragePreliminaryFailure.PathNotFound => StorageResponse.OfError(StorageErrorType.FileNotFound),
                RecurseOverStoragePreliminaryFailure.PathNotDirectory => StorageResponse.OfError(StorageErrorType.PathIsNotDirectory),
                _ => throw new ArgumentOutOfRangeException()
            }
        );
    }
    
    public StorageResponse<StorageFile> Read(string path) {
        var pathParts = IStorageLayer.CleanPath(path);
        return RecurseOverStorage(
            pathParts,
            _storage,
            (substorage, entryName) => {
                if (!substorage.ContainsKey(entryName)) {
                    return StorageResponse<StorageFile>.OfError(StorageErrorType.FileNotFound);
                }
                
                if (substorage[entryName] is not byte[] data) {
                    return StorageResponse<StorageFile>.OfError(StorageErrorType.PathIsNotFile);
                }
                
                var readData = new byte[data.Length];
                data.CopyTo(readData, 0);
                
                return StorageResponse<StorageFile>.OfSuccess(StorageFile.Of(entryName, readData, LayerName));
            },
            preliminaryFailure => preliminaryFailure switch {
                RecurseOverStoragePreliminaryFailure.PathNotFound => StorageResponse<StorageFile>.OfError(StorageErrorType.FileNotFound),
                RecurseOverStoragePreliminaryFailure.PathNotDirectory => StorageResponse<StorageFile>.OfError(StorageErrorType.PathIsNotDirectory),
                _ => throw new ArgumentOutOfRangeException()
            }
        );
    }
    
    public StorageResponse<StorageFileMetadata> ReadMetadata(string path) {
        var pathParts = IStorageLayer.CleanPath(path);
        return RecurseOverStorage(
            pathParts,
            _storage,
            (substorage, entryName) => {
                if (!substorage.ContainsKey(entryName)) {
                    return StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.FileNotFound);
                }
                
                return substorage[entryName] switch {
                    IDictionary<string, object> => StorageResponse<StorageFileMetadata>.OfSuccess(StorageFileMetadata.OfDirectory(
                        entryName, 
                        LayerName
                    )),
                    byte[] data => StorageResponse<StorageFileMetadata>.OfSuccess(StorageFileMetadata.OfFile(
                        entryName, 
                        data.Length, 
                        LayerName
                    )),
                    _ => StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.PathIsNotFile)
                };
            },
            preliminaryFailure => preliminaryFailure switch {
                RecurseOverStoragePreliminaryFailure.PathNotFound => StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.FileNotFound),
                RecurseOverStoragePreliminaryFailure.PathNotDirectory => StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.PathIsNotDirectory),
                _ => throw new ArgumentOutOfRangeException()
            }
        );
    }
    
    public StorageResponse<StorageFileMetadata[]> List(string path) {
        var pathParts = IStorageLayer.CleanPath(path);
        return RecurseOverStorage(
            pathParts,
            _storage,
            (substorage, entryName) => {
                if (!substorage.ContainsKey(entryName)) {
                    return StorageResponse<StorageFileMetadata[]>.OfError(StorageErrorType.DirectoryNotFound);
                }
                
                if (substorage[entryName] is not IDictionary<string, object> directory) {
                    return StorageResponse<StorageFileMetadata[]>.OfError(StorageErrorType.PathIsNotDirectory);
                }
                
                var metadataList = directory
                    .Select(entry => {
                        var (name, value) = entry;
                        return value switch {
                            IDictionary<string, object> _ => StorageFileMetadata.OfDirectory(name, LayerName),
                            byte[] data => StorageFileMetadata.OfFile(name, data.Length, LayerName),
                            _ => null // Should never happen, but just in case
                        };
                    })
                    .WhereNotNull()
                    .ToArray();
                
                return StorageResponse<StorageFileMetadata[]>.OfSuccess(metadataList);
            },
            preliminaryFailure => preliminaryFailure switch {
                RecurseOverStoragePreliminaryFailure.PathNotFound => StorageResponse<StorageFileMetadata[]>.OfError(StorageErrorType.DirectoryNotFound),
                RecurseOverStoragePreliminaryFailure.PathNotDirectory => StorageResponse<StorageFileMetadata[]>.OfError(StorageErrorType.PathIsNotDirectory),
                _ => throw new ArgumentOutOfRangeException()
            }
        );
    }
    
    public StorageResponse Write(string path, byte[] value) {
        var pathParts = IStorageLayer.CleanPath(path);
        return RecurseOverStorage(
            pathParts,
            _storage,
            (substorage, entryName) => {
                if (substorage.ContainsKey(entryName)) {
                    return StorageResponse.OfError(StorageErrorType.FileAlreadyExists);
                }
                
                var writeData = new byte[value.Length];
                value.CopyTo(writeData, 0);
                
                substorage[entryName] = writeData;
                return StorageResponse.OfSuccess();
            },
            preliminaryFailure => preliminaryFailure switch {
                RecurseOverStoragePreliminaryFailure.PathNotFound => StorageResponse.OfError(StorageErrorType.FileNotFound),
                RecurseOverStoragePreliminaryFailure.PathNotDirectory => StorageResponse.OfError(StorageErrorType.PathIsNotDirectory),
                _ => throw new ArgumentOutOfRangeException()
            }
        );
    }
    
    private enum RecurseOverStoragePreliminaryFailure {
        PathNotFound,
        PathNotDirectory
    }

    private static T RecurseOverStorage<T>(
        string[] pathParts, 
        IDictionary<string, object> storage, 
        Func<IDictionary<string, object>, string, T> processSubstorage,
        Func<RecurseOverStoragePreliminaryFailure, T> preliminaryFailure
    ) {
        while (true) {
            if (pathParts.Length == 0) {
                return preliminaryFailure(RecurseOverStoragePreliminaryFailure.PathNotFound);
            }
            
            var currentPart = pathParts[0];
            
            if (pathParts.Length == 1) {
                return processSubstorage(storage, currentPart);
            }
            
            if (!storage.ContainsKey(currentPart)) {
                return preliminaryFailure(RecurseOverStoragePreliminaryFailure.PathNotFound);
            }
            
            if (storage[currentPart] is not IDictionary<string, object> directory) {
                return preliminaryFailure(RecurseOverStoragePreliminaryFailure.PathNotDirectory);
            }
            
            pathParts = pathParts[1..];
            storage = directory;
        }
    }
}
