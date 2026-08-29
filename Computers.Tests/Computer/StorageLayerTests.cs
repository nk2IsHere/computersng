using System.Text;
using Computers.Computer;
using Computers.Computer.Domain.Storage;
using Computers.Tests.TestDoubles;
using Xunit;

namespace Computers.Tests.Computer;

public class PersistentStorageLayerTests {
    private static PersistentStorageLayer Empty() => new(new Dictionary<string, object>());

    [Fact]
    public void WriteThenReadRoundTrips() {
        var layer = Empty();
        var data = Encoding.UTF8.GetBytes("content");

        Assert.Equal(StorageResponseType.Success, layer.Write("/file.txt", data).Type);
        var read = layer.Read("/file.txt");
        Assert.Equal(StorageResponseType.Success, read.Type);
        Assert.Equal(data, read.Data!.Data);
    }

    [Fact]
    public void NestedDirectoriesWork() {
        var layer = Empty();
        Assert.Equal(StorageResponseType.Success, layer.MakeDirectory("/a").Type);
        Assert.Equal(StorageResponseType.Success, layer.MakeDirectory("/a/b").Type);
        Assert.Equal(StorageResponseType.Success, layer.Write("/a/b/file.txt", new byte[] { 1 }).Type);
        Assert.True(layer.Exists("/a/b/file.txt"));

        var metadata = layer.ReadMetadata("/a/b");
        Assert.Equal(StorageResponseType.Success, metadata.Type);
        Assert.Equal(StorageFileType.Directory, metadata.Data!.Type);
    }

    [Fact]
    public void ReadMissingFileFails() {
        Assert.Equal(StorageErrorType.FileNotFound, Empty().Read("/nope").Error);
    }

    [Fact]
    public void ReadDirectoryAsFileFails() {
        var layer = Empty();
        layer.MakeDirectory("/dir");
        Assert.Equal(StorageErrorType.PathIsNotFile, layer.Read("/dir").Error);
    }

    [Fact]
    public void MakeExistingDirectoryFails() {
        var layer = Empty();
        layer.MakeDirectory("/dir");
        Assert.Equal(StorageErrorType.DirectoryAlreadyExists, layer.MakeDirectory("/dir").Error);
    }

    [Fact]
    public void DeleteNonEmptyDirectoryRequiresRecursive() {
        var layer = Empty();
        layer.MakeDirectory("/dir");
        layer.Write("/dir/file.txt", new byte[] { 1 });

        Assert.Equal(StorageErrorType.DirectoryNotEmpty, layer.Delete("/dir").Error);
        Assert.Equal(StorageResponseType.Success, layer.Delete("/dir", recursive: true).Type);
        Assert.False(layer.Exists("/dir"));
    }

    [Fact]
    public void DeleteMissingEntryFails() {
        Assert.Equal(StorageErrorType.FileNotFound, Empty().Delete("/nope").Error);
    }

    [Fact]
    public void ListReturnsFilesAndDirectoriesOfASubdirectory() {
        var layer = Empty();
        layer.MakeDirectory("/dir");
        layer.MakeDirectory("/dir/sub");
        layer.Write("/dir/file.txt", new byte[] { 1, 2 });

        var list = layer.List("/dir");
        Assert.Equal(StorageResponseType.Success, list.Type);
        Assert.Equal(2, list.Data!.Length);
        Assert.Contains(list.Data, entry => entry.Name == "sub" && entry.Type == StorageFileType.Directory);
        Assert.Contains(list.Data, entry => entry.Name == "file.txt" && entry.Type == StorageFileType.File);
    }

    [Fact]
    public void ListingTheRootWorks() {
        var layer = Empty();
        layer.MakeDirectory("/dir");
        layer.Write("/file.txt", new byte[] { 1 });

        var list = layer.List("/");
        Assert.Equal(StorageResponseType.Success, list.Type);
        Assert.Equal(2, list.Data!.Length);
        Assert.Contains(list.Data, entry => entry.Name == "dir" && entry.Type == StorageFileType.Directory);
        Assert.Contains(list.Data, entry => entry.Name == "file.txt" && entry.Type == StorageFileType.File);
    }

    [Fact]
    public void WritesReachTheBackingDictionary() {
        // Regression: the constructor used to copy the dictionary, so writes never reached
        // the storage that ComputerStatefulDataContextEntry.Store() serializes into the save.
        var backing = new Dictionary<string, object>();
        var layer = new PersistentStorageLayer(backing);

        layer.Write("/file.txt", new byte[] { 1, 2 });
        layer.MakeDirectory("/dir");
        layer.Write("/dir/nested.txt", new byte[] { 3 });

        Assert.True(backing.ContainsKey("file.txt"));
        var dir = Assert.IsAssignableFrom<IDictionary<string, object>>(backing["dir"]);
        Assert.True(dir.ContainsKey("nested.txt"));
    }
}

public class LoaderStorageLayerTests {
    private static LoaderStorageLayer Layer() => new(
        new InMemoryRedundantLoader(new Dictionary<string, string> {
            ["Core/Engine.js"] = "export {}",
            ["Core/Utils/Cache.js"] = "export {}"
        }),
        "TestLayer"
    );

    [Fact]
    public void ReadsExistingFile() {
        var read = Layer().Read("Core/Engine.js");
        Assert.Equal(StorageResponseType.Success, read.Type);
        Assert.Equal("export {}", Encoding.UTF8.GetString(read.Data!.Data));
        Assert.Equal("TestLayer", read.Data.Metadata.Layer);
    }

    [Fact]
    public void MissingFileReportsFileNotFound() {
        Assert.Equal(StorageErrorType.FileNotFound, Layer().Read("Core/Nope.js").Error);
    }

    [Fact]
    public void ListsDirectory() {
        var list = Layer().List("Core");
        Assert.Equal(StorageResponseType.Success, list.Type);
        Assert.Contains(list.Data!, entry => entry.Name == "Engine.js");
    }

    [Fact]
    public void WritesAndDeletesAreRefused() {
        Assert.Equal(StorageErrorType.ReadOnlyLayer, Layer().Write("Core/New.js", new byte[] { 1 }).Error);
        Assert.Equal(StorageErrorType.ReadOnlyLayer, Layer().Delete("Core/Engine.js").Error);
        Assert.Equal(StorageErrorType.ReadOnlyLayer, Layer().MakeDirectory("Core/Sub").Error);
    }
}

public class CompositeStorageLayerTests {
    private static LoaderStorageLayer ReadOnly(Dictionary<string, string> files, int priority = 0) =>
        new(new InMemoryRedundantLoader(files), "ReadOnly", priority);

    private static CompositeStorageLayer Composite(out PersistentStorageLayer readWrite) {
        readWrite = new PersistentStorageLayer(new Dictionary<string, object>());
        return new CompositeStorageLayer(new IStorageLayer[] {
            ReadOnly(new Dictionary<string, string> { ["shadowed.txt"] = "from-readonly" }, priority: 1),
            readWrite
        });
    }

    [Fact]
    public void TwoReadWriteLayersAreRejected() {
        Assert.Throws<InvalidOperationException>(() => new CompositeStorageLayer(new IStorageLayer[] {
            new PersistentStorageLayer(new Dictionary<string, object>(), priority: 1),
            new PersistentStorageLayer(new Dictionary<string, object>(), priority: 2)
        }));
    }

    [Fact]
    public void ReadWriteLayerMustBeLast() {
        Assert.Throws<InvalidOperationException>(() => new CompositeStorageLayer(new IStorageLayer[] {
            new PersistentStorageLayer(new Dictionary<string, object>(), priority: 0),
            ReadOnly(new Dictionary<string, string>(), priority: 1)
        }));
    }

    [Fact]
    public void HigherPriorityLayerShadowsOnRead() {
        var composite = Composite(out var readWrite);
        readWrite.Write("shadowed.txt", Encoding.UTF8.GetBytes("from-readwrite"));

        var read = composite.Read("shadowed.txt");
        Assert.Equal(StorageResponseType.Success, read.Type);
        Assert.Equal("from-readonly", Encoding.UTF8.GetString(read.Data!.Data));
    }

    [Fact]
    public void WritesGoToTheReadWriteLayer() {
        var composite = Composite(out var readWrite);
        Assert.Equal(StorageResponseType.Success, composite.Write("new.txt", new byte[] { 1 }).Type);
        Assert.True(readWrite.Exists("new.txt"));
    }

    [Fact]
    public void WritingOverAnyExistingEntryFails() {
        var composite = Composite(out _);
        Assert.Equal(StorageErrorType.FileAlreadyExists, composite.Write("shadowed.txt", new byte[] { 1 }).Error);
    }

    [Fact]
    public void DeletingReadOnlyOnlyEntriesFails() {
        var composite = Composite(out _);
        Assert.Equal(StorageErrorType.ReadOnlyLayer, composite.Delete("shadowed.txt").Error);
    }

    [Fact]
    public void ListMergesLayersAndDeduplicatesByName() {
        var readWrite = new PersistentStorageLayer(new Dictionary<string, object>());
        var composite = new CompositeStorageLayer(new IStorageLayer[] {
            ReadOnly(new Dictionary<string, string> { ["dir/shadowed.txt"] = "from-readonly" }, priority: 1),
            readWrite
        });
        readWrite.MakeDirectory("dir");
        readWrite.Write("dir/shadowed.txt", new byte[] { 9 });
        readWrite.Write("dir/only-rw.txt", new byte[] { 9 });

        var list = composite.List("dir");
        Assert.Equal(StorageResponseType.Success, list.Type);
        Assert.Equal(2, list.Data!.Length); // shadowed.txt deduplicated
        Assert.Contains(list.Data, entry => entry.Name == "only-rw.txt");
    }
}
