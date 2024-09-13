namespace Computers.Computer;

public enum StorageErrorType {
    FileNotFound,
    DirectoryNotFound,
    FileAlreadyExists,
    DirectoryAlreadyExists,
    DirectoryNotEmpty,
    PathIsNotDirectory,
    PathIsNotFile,
    ReadOnlyLayer
}

public enum StorageResponseType {
    Success,
    Error
}

public class StorageResponse<T> {
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

public class StorageResponse {
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


public enum StorageFileType {
    File,
    Directory
}

public class StorageFileMetadata {
    public string Name { get; }
    public StorageFileType Type { get; }
    public long Size { get; }

    public string Layer { get; }

    private StorageFileMetadata(string name, StorageFileType type, long size, string layer) {
        Name = name;
        Type = type;
        Size = size;
        Layer = layer;
    }
    
    public override string ToString() {
        return $"StorageFileMetadata(Name: {Name}, Type: {Type}, Size: {Size}, Layer: {Layer})";
    }
    
    public static StorageFileMetadata Of(string name, StorageFileType type, long size, string layer) {
        return new StorageFileMetadata(name, type, size, layer);
    }
}

public class StorageFile {
    public StorageFileMetadata Metadata { get; }
    public byte[] Data { get; }
    
    private StorageFile(StorageFileMetadata metadata, byte[] data) {
        Metadata = metadata;
        Data = data;
    }
    
    public override string ToString() {
        return $"StorageFile(Metadata: {Metadata})";
    }
    
    public static StorageFile Of(string name, byte[] data, string layer) {
        return new StorageFile(StorageFileMetadata.Of(name, StorageFileType.File, data.Length, layer), data);
    }
}

public enum StorageLayerMode {
    ReadOnly,
    ReadWrite
}

public interface IStorageLayer {
    
    public const string Separator = "/";
    
    public static string[] CleanPath(string path) {
        return path.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
    }
    
    public int Priority { get; }
    public StorageLayerMode Mode { get; }
    
    bool Exists(string path);
    StorageResponse MakeDirectory(string path);
    StorageResponse Delete(string path, bool recursive = false);
    StorageResponse<StorageFile> Read(string path);
    StorageResponse<StorageFileMetadata> ReadMetadata(string path);
    StorageResponse<StorageFileMetadata[]> List(string path);
    StorageResponse Write(string path, byte[] value);
}
