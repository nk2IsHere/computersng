using System.Text;
using Computers.Game;

namespace Computers.Computer.Domain.Storage;

public class CoreLibraryStorageLayer: IStorageLayer {
    
    private readonly IRedundantLoader _coreLibraryLoader;

    public CoreLibraryStorageLayer(IRedundantLoader coreLibraryLoader) {
        _coreLibraryLoader = coreLibraryLoader;
    }

    public int Priority => 0;
    public StorageLayerMode Mode => StorageLayerMode.ReadOnly;
    
    public bool Exists(string path) {
        return _coreLibraryLoader.Exists(path);
    }

    public StorageResponse MakeDirectory(string path) {
        return StorageResponse.OfError(StorageErrorType.ReadOnlyLayer);
    }

    public StorageResponse Delete(string path, bool recursive = false) {
        return StorageResponse.OfError(StorageErrorType.ReadOnlyLayer);
    }

    public StorageResponse<StorageFile> Read(string path) {
        try {
            var file = _coreLibraryLoader.Load<string>(path);
            var fileBytes = Encoding.UTF8.GetBytes(file);
            return StorageResponse<StorageFile>.OfSuccess(StorageFile.Of(path, fileBytes));
        } catch (Exception) {
            return StorageResponse<StorageFile>.OfError(StorageErrorType.FileNotFound);
        }
    }

    public StorageResponse<StorageFileMetadata> ReadMetadata(string path) {
        try {
            var file = _coreLibraryLoader.Load<string>(path);
            var fileBytes = Encoding.UTF8.GetBytes(file);
            return StorageResponse<StorageFileMetadata>.OfSuccess(StorageFileMetadata.Of(path, StorageFileType.File, fileBytes.Length));
        } catch (Exception) {
            return StorageResponse<StorageFileMetadata>.OfError(StorageErrorType.FileNotFound);
        }
    }

    public StorageResponse<StorageFileMetadata[]> List(string path) {
        try {
            var metadata = _coreLibraryLoader.List(path)
                .Select(file => StorageFileMetadata.Of(file, StorageFileType.File, 0))
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
