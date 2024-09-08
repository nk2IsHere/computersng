using System.Text;
using Computers.Computer.Boundary;
using Computers.Game.Boundary;

namespace Computers.Computer;

public class StorageComputerApi: IComputerApi {
    public string Name => "Storage";
    public bool ShouldExpose => true;
    public object Api => _state;
    
    public ISet<Type> ReceivableEvents => new HashSet<Type>();
    public IRedundantLoader LibraryLoader => new StorageRedundantLoader(_state);
    
    private readonly StorageComputerState _state;

    public StorageComputerApi(IComputerPort computerPort) {
        _state = new StorageComputerState(computerPort.GetStorage(this));
    }
    
    public void ReceiveEvent(IComputerEvent computerEvent) {
    }

    public void Reset() {
    }
}

internal enum StorageErrorType {
    FileNotFound,
    DirectoryNotFound,
    FileAlreadyExists,
    DirectoryAlreadyExists,
    DirectoryNotEmpty,
    PathIsNotDirectory,
    PathIsNotFile
}

internal enum StorageResponseType {
    Success,
    Error
}

internal class StorageResponse<T> {
    public StorageResponseType Type { get; }
    public StorageErrorType? Error { get; }
    public T? Data { get; }
    
    private StorageResponse(StorageResponseType type, StorageErrorType? error, T? data) {
        Type = type;
        Error = error;
        Data = data;
    }
    
    public static StorageResponse<T> OfSuccess(T data) {
        return new StorageResponse<T>(StorageResponseType.Success, null, data);
    }
    
    public static StorageResponse<T> OfError(StorageErrorType error) {
        return new StorageResponse<T>(StorageResponseType.Error, error, default);
    }

    public override string ToString() {
        return Type switch {
            StorageResponseType.Success => $"StorageResponse(Success: {Data})",
            StorageResponseType.Error => $"StorageResponse(Error: {Error})",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

internal class StorageResponse {
    public StorageResponseType Type { get; }
    public StorageErrorType? Error { get; }
    
    private StorageResponse(StorageResponseType type, StorageErrorType? error) {
        Type = type;
        Error = error;
    }
    
    public static StorageResponse OfSuccess() {
        return new StorageResponse(StorageResponseType.Success, null);
    }
    
    public static StorageResponse OfError(StorageErrorType error) {
        return new StorageResponse(StorageResponseType.Error, error);
    }
    
    public override string ToString() {
        return Type switch {
            StorageResponseType.Success => "StorageResponse(Success)",
            StorageResponseType.Error => $"StorageResponse(Error: {Error})",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}


internal enum StorageFileType {
    File,
    Directory
}

internal class StorageFileMetadata {
    public string Name { get; }
    public StorageFileType Type { get; }
    public long Size { get; }
    
    private StorageFileMetadata(string name, StorageFileType type, long size) {
        Name = name;
        Type = type;
        Size = size;
    }
    
    public override string ToString() {
        return $"StorageFileMetadata(Name: {Name}, Type: {Type}, Size: {Size})";
    }
    
    public static StorageFileMetadata Of(string name, StorageFileType type, long size) {
        return new StorageFileMetadata(name, type, size);
    }
}

internal class StorageFile {
    public StorageFileMetadata Metadata { get; }
    public byte[] Data { get; }
    
    private StorageFile(StorageFileMetadata metadata, byte[] data) {
        Metadata = metadata;
        Data = data;
    }
    
    public override string ToString() {
        return $"StorageFile(Metadata: {Metadata})";
    }
    
    public static StorageFile Of(string name, byte[] data) {
        return new StorageFile(StorageFileMetadata.Of(name, StorageFileType.File, data.Length), data);
    }
}

internal class StorageComputerState {
    
    public const string Separator = "/";
    
    private readonly Dictionary<string, object> _storage;
    
    public StorageComputerState(IDictionary<string, object> storage) {
        _storage = new Dictionary<string, object>(storage);
    }
    
    public bool Exists(string path) {
        var pathParts = CleanPath(path);
        return ExistsRecursive(pathParts, _storage);
    }
    
    public StorageResponse MakeDirectory(string path) {
        var pathParts = CleanPath(path);
        return MakeDirectoryRecursive(pathParts, _storage);
    }
    
    public StorageResponse Delete(string path, bool recursive = false) {
        var pathParts = CleanPath(path);
        return DeleteRecursive(pathParts, recursive, _storage);
    }
    
    public StorageResponse<StorageFile> Read(string path) {
        var pathParts = CleanPath(path);
        return ReadRecursive(pathParts, _storage);
    }
    
    public StorageResponse<StorageFileMetadata[]> List(string path) {
        var pathParts = CleanPath(path);
        return ListRecursive(pathParts, _storage);
    }
    
    public StorageResponse Write(string path, byte[] value) {
        var pathParts = CleanPath(path);
        return WriteRecursive(pathParts, _storage, value);
    }
    
    private static string[] CleanPath(string path) {
        return path.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
    }
    
    private static StorageResponse<StorageFileMetadata[]> ListRecursive(string[] pathParts, IDictionary<string, object> storage) {
        while (true) {
            if (pathParts.Length == 0) {
                var metadataList = storage
                    .Select(entry => {
                        var (name, value) = entry;
                        return value switch {
                            byte[] data => StorageFileMetadata.Of(name, StorageFileType.File, data.Length),
                            IDictionary<string, object> _ => StorageFileMetadata.Of(name, StorageFileType.Directory, 0),
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
                
                return StorageResponse<StorageFile>.OfSuccess(StorageFile.Of(currentPart, readData));

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
                    storage[currentPart] is IDictionary<string, object> directoryToDelete
                    && directoryToDelete.Count > 0 
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

    public bool Exists(string path) {
        return _state.Exists(path);
    }
}