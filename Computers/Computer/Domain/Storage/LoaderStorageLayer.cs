using System.Text;
using Computers.Game;

namespace Computers.Computer.Domain.Storage;

public class LoaderStorageLayer: IStorageLayer {
    
    private readonly IRedundantLoader _loader;

    public LoaderStorageLayer(IRedundantLoader loader, int priority = 0) {
        _loader = loader;
        Priority = priority;
    }

    public int Priority { get; }
    public StorageLayerMode Mode => StorageLayerMode.ReadOnly;
    
    public bool Exists(string path) {
        return _loader.Exists(path);
    }

    public StorageResponse MakeDirectory(string path) {
        return StorageResponse.OfError(StorageErrorType.ReadOnlyLayer);
    }

    public StorageResponse Delete(string path, bool recursive = false) {
        return StorageResponse.OfError(StorageErrorType.ReadOnlyLayer);
    }

    public StorageResponse<StorageFile> Read(string path) {
        try {
            var file = _loader.Load<string>(path);
            var fileBytes = Encoding.UTF8.GetBytes(file);
            return StorageResponse<StorageFile>.OfSuccess(StorageFile.Of(path, fileBytes));
        } catch (Exception) {
            return StorageResponse<StorageFile>.OfError(StorageErrorType.FileNotFound);
        }
    }

    public StorageResponse<StorageFileMetadata> ReadMetadata(string path) {
        try {
            var file = _loader.Load<string>(path);
            var fileBytes = Encoding.UTF8.GetBytes(file);
            return StorageResponse<StorageFileMetadata>.OfSuccess(StorageFileMetadata.Of(path, StorageFileType.File, fileBytes.Length));
        } catch (Exception) {
            return StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.FileNotFound);
        }
    }

    public StorageResponse<StorageFileMetadata[]> List(string path) {
        try {
            var metadata = _loader.List(path)
                .Select(entry => StorageFileMetadata.Of(
                    entry.Name,
                    entry.Type switch {
                        FileSystemEntryType.File => StorageFileType.File,
                        FileSystemEntryType.Directory => StorageFileType.Directory,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    entry.Size
                ))
                .ToArray();
            
            return StorageResponse<StorageFileMetadata[]>.OfSuccess(metadata);
        } catch (Exception) {
            return StorageResponse<StorageFileMetadata[]>.OfError(StorageErrorType.FileNotFound);
        }
    }

    public StorageResponse Write(string path, byte[] value) {
        return StorageResponse.OfError(StorageErrorType.ReadOnlyLayer);
    }
}
