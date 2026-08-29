using Computers.Game.Domain;
using Xunit;
using Object = StardewValley.Object;

namespace Computers.Tests.Game;

public class DiskItemPatchesTests {
    private static Object Disk(string? computerId) {
        var disk = new Object();
        if (computerId is not null) {
            disk.modData["ComputerId"] = computerId;
        }
        return disk;
    }

    [Fact]
    public void BlankDisksStillStack() {
        var result = true;
        DiskItemPatches.CanStackWithPostfix(Disk(null), Disk(null), ref result);
        Assert.True(result);
    }

    [Fact]
    public void DisksWithDifferentIdsDoNotStack() {
        var result = true;
        DiskItemPatches.CanStackWithPostfix(Disk("a.b.disk1"), Disk("a.b.disk2"), ref result);
        Assert.False(result);
    }

    [Fact]
    public void InitializedAndBlankDiskDoNotStack() {
        var result = true;
        DiskItemPatches.CanStackWithPostfix(Disk("a.b.disk1"), Disk(null), ref result);
        Assert.False(result);
    }

    [Fact]
    public void SameIdRemainsStackable() {
        // Same id on two items only happens for split stacks of the same disk.
        var result = true;
        DiskItemPatches.CanStackWithPostfix(Disk("a.b.disk1"), Disk("a.b.disk1"), ref result);
        Assert.True(result);
    }

    [Fact]
    public void FalseResultStaysFalse() {
        var result = false;
        DiskItemPatches.CanStackWithPostfix(Disk(null), Disk(null), ref result);
        Assert.False(result);
    }

    [Fact]
    public void DescriptionGainsIdSuffix() {
        var description = "A disk.";
        DiskItemPatches.GetDescriptionPostfix(Disk("tools.kot.nk2.computers.Data.Computer.abc123"), ref description);
        Assert.EndsWith("ID: abc123", description);
    }

    [Fact]
    public void DescriptionUntouchedWithoutId() {
        var description = "A disk.";
        DiskItemPatches.GetDescriptionPostfix(Disk(null), ref description);
        Assert.Equal("A disk.", description);
    }
}
