using Computers.Core;
using Computers.Game;
using Computers.Game.Domain;
using Computers.Game.Utils;
using Computers.Tests.TestDoubles;
using StardewModdingAPI.Enums;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using Xunit;
using Object = StardewValley.Object;

namespace Computers.Tests.Game;

public class RecipeTests {
    [Fact]
    public void PatchStringMatchesGameFormat() {
        var recipe = new Recipe(
            "some.yield.id",
            new Dictionary<string, int> { ["380"] = 5, ["388"] = 2 },
            IsBigCraftable: true,
            new IRecipeRequirement.NoneRequired(),
            "Computer"
        );

        Assert.Equal("380 5 388 2/Field/some.yield.id/True/default/", recipe.PatchString);
        Assert.Equal("Computer", recipe.PatchKey);
    }

    [Fact]
    public void SkillRequirementRendersSkillAndLevel() {
        var requirement = new IRecipeRequirement.SkillRequired(SkillType.Farming, 3);
        Assert.Equal("Farming 3", requirement.PatchString);
    }
}

public class ResourceUtilsTests {
    [Fact]
    public void SplitPathHandlesBothSeparatorsAndEmptySegments() {
        Assert.Equal(new[] { "a", "b", "c" }, ResourceUtils.SplitPath("a/b\\c"));
        Assert.Equal(new[] { "a", "b" }, ResourceUtils.SplitPath("/a//b/"));
        Assert.Empty(ResourceUtils.SplitPath("/"));
    }

    [Fact]
    public void ResolvePathJoinsBaseAndRelative() {
        Assert.Equal(Path.Combine("base", "sub", "file.js"), ResourceUtils.ResolvePath("base", "sub/file.js"));
    }
}

public class EventBusTests {
    private record TestEvent(string Payload) : IEvent {
        public Type EventType => typeof(TestEvent);
        public T Data<T>() => (T) (object) Payload;
    }

    private record OtherEvent : IEvent {
        public Type EventType => typeof(OtherEvent);
        public T Data<T>() => default!;
    }

    private class RecordingHandler : IEventHandler {
        public List<IEvent> Received { get; } = new();
        public ISet<Type> EventTypes => new HashSet<Type> { typeof(TestEvent) };
        public void Handle(IEvent @event) => Received.Add(@event);
    }

    [Fact]
    public void DispatchesOnlyToHandlersDeclaringTheEventType() {
        var handler = new RecordingHandler();
        var bus = new EventBus(
            new TestMonitor(),
            new ContextLookup<IEventHandler>(
                () => new HashSet<ContextEntry<IEventHandler>> { new("handler.a".AsId(), handler) })
        );

        bus.Publish(new TestEvent("hello"));
        bus.Publish(new OtherEvent());

        var received = Assert.Single(handler.Received);
        Assert.Equal("hello", received.Data<string>());
    }
}

public class ObjectUtilsTests {
    [Fact]
    public void HeldObjectModDataIsNullWithoutHeldObject() {
        Assert.Null(new Object().HeldObjectModData());
    }

    [Fact]
    public void HeldObjectModDataReturnsHeldObjectsData() {
        var machine = new Object();
        var disk = new Object();
        disk.modData["ComputerId"] = "some.id";
        machine.heldObject.Value = disk;

        var modData = machine.HeldObjectModData();
        Assert.NotNull(modData);
        Assert.Equal("some.id", modData!["ComputerId"]);
    }
}

public class PatcherServicesTests {
    private static ISet<ContextEntry<T>> Entries<T>(Id id, T value) =>
        new HashSet<ContextEntry<T>> { new(id, value) };

    [Fact]
    public void CanPatchGatesOnAssetTypeAndName() {
        var monitor = new TestMonitor();
        var recipePatcher = new RecipePatcherService(monitor, new HashSet<ContextEntry<Recipe>>());
        var bigCraftablePatcher = new BigCraftablePatcherService(monitor, new HashSet<ContextEntry<BigCraftableData>>());
        var machinePatcher = new MachinePatcherService(monitor, new HashSet<ContextEntry<Machine>>());
        var objectPatcher = new ObjectPatcherService(monitor, new HashSet<ContextEntry<ObjectData>>());

        Assert.True(recipePatcher.CanPatch(typeof(Dictionary<string, string>), FakeAssetName.Of("Data/CraftingRecipes")));
        Assert.True(bigCraftablePatcher.CanPatch(typeof(Dictionary<string, BigCraftableData>), FakeAssetName.Of("Data/BigCraftables")));
        Assert.True(machinePatcher.CanPatch(typeof(Dictionary<string, MachineData>), FakeAssetName.Of("Data/Machines")));
        Assert.True(objectPatcher.CanPatch(typeof(Dictionary<string, ObjectData>), FakeAssetName.Of("Data/Objects")));

        // Wrong name and wrong type are both rejected.
        Assert.False(recipePatcher.CanPatch(typeof(Dictionary<string, string>), FakeAssetName.Of("Data/Objects")));
        Assert.False(recipePatcher.CanPatch(typeof(Dictionary<string, int>), FakeAssetName.Of("Data/CraftingRecipes")));
    }

    [Fact]
    public void RecipePatcherAddsPatchEntries() {
        var recipe = new Recipe(
            "yield.id",
            new Dictionary<string, int> { ["380"] = 1 },
            IsBigCraftable: false,
            new IRecipeRequirement.NoneRequired(),
            "Disk"
        );
        var patcher = new RecipePatcherService(new TestMonitor(), Entries("recipe.disk".AsId(), recipe));

        var assetData = new Dictionary<string, string> { ["Existing"] = "kept" };
        patcher.Patch(new FakeAssetData("Data/CraftingRecipes", assetData));

        Assert.Equal("kept", assetData["Existing"]);
        Assert.Equal(recipe.PatchString, assetData["Disk"]);
    }

    [Fact]
    public void BigCraftablePatcherAddsDataUnderEntryId() {
        var data = new BigCraftableData { Name = "Computer", DisplayName = "Computer" };
        var patcher = new BigCraftablePatcherService(
            new TestMonitor(), Entries("bc.computer".AsId(), data));

        var assetData = new Dictionary<string, BigCraftableData>();
        patcher.Patch(new FakeAssetData("Data/BigCraftables", assetData));

        Assert.Same(data, assetData["bc.computer"]);
    }
}
