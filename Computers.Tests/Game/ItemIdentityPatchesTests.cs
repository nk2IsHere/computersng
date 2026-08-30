using Computers.Game.Domain;
using Computers.Game.Patch;
using Xunit;
using Object = StardewValley.Object;

namespace Computers.Tests.Game;

public class ItemIdentityPatchesTests {
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
        ItemIdentityPatches.CanStackWithPostfix(Disk(null), Disk(null), ref result);
        Assert.True(result);
    }

    [Fact]
    public void DisksWithDifferentIdsDoNotStack() {
        var result = true;
        ItemIdentityPatches.CanStackWithPostfix(Disk("a.b.disk1"), Disk("a.b.disk2"), ref result);
        Assert.False(result);
    }

    [Fact]
    public void InitializedAndBlankDiskDoNotStack() {
        var result = true;
        ItemIdentityPatches.CanStackWithPostfix(Disk("a.b.disk1"), Disk(null), ref result);
        Assert.False(result);
    }

    [Fact]
    public void SameIdRemainsStackable() {
        // Same id on two items only happens for split stacks of the same disk.
        var result = true;
        ItemIdentityPatches.CanStackWithPostfix(Disk("a.b.disk1"), Disk("a.b.disk1"), ref result);
        Assert.True(result);
    }

    private static Object WithKey(string key, string? id) {
        var item = new Object();
        if (id is not null) {
            item.modData[key] = id;
        }
        return item;
    }

    [Fact]
    public void RoutersWithDifferentIdsDoNotStack() {
        var result = true;
        ItemIdentityPatches.CanStackWithPostfix(
            WithKey("RouterId", "a.b.r1"), WithKey("RouterId", "a.b.r2"), ref result);
        Assert.False(result);
    }

    [Fact]
    public void PeripheralWithIdDoesNotStackWithBlankOne() {
        var result = true;
        ItemIdentityPatches.CanStackWithPostfix(
            WithKey("PeripheralId", "a.b.p1"), WithKey("PeripheralId", null), ref result);
        Assert.False(result);
    }

    [Fact]
    public void FalseResultStaysFalse() {
        var result = false;
        ItemIdentityPatches.CanStackWithPostfix(Disk(null), Disk(null), ref result);
        Assert.False(result);
    }

    [Fact]
    public void DescriptionGainsIdSuffix() {
        var description = "A disk.";
        ItemIdentityPatches.GetDescriptionPostfix(Disk("tools.kot.nk2.computers.Data.Computer.abc123"), ref description);
        Assert.EndsWith("ID: abc123", description);
    }

    [Fact]
    public void DescriptionUntouchedWithoutId() {
        var description = "A disk.";
        ItemIdentityPatches.GetDescriptionPostfix(Disk(null), ref description);
        Assert.Equal("A disk.", description);
    }
}
