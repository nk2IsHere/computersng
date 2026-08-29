using Computers.Core;
using Xunit;

namespace Computers.Tests.Core;

public class ContextTests {
    private static readonly Id ServiceId = "svc.a".AsId();
    private static readonly Id FactoryId = "svc.factory".AsId();
    private static readonly Id BaseEntryId = "data.counter".AsId();

    // Minimal stateful entry + factory mirroring the computer/router pattern.
    private class CounterEntry : IContextEntry.StatefulDataContextEntry<CounterEntry> {
        public int Value { get; set; }

        public CounterEntry(Id factoryId, Id id) : base(factoryId, id) {
        }

        public override object GetValue(Context context) => this;

        public override void Restore(Context context, ContextEntryState state) {
            Value = Convert.ToInt32(state.GetOrDefault<object>("Value", 0));
        }

        public override ContextEntryState Store(Context context) {
            var state = ContextEntryState.Empty;
            state.Id = Id;
            state.FactoryId = FactoryId;
            state.Set("Value", Value);
            return state;
        }
    }

    private class CounterFactory : IStatefulDataContextEntryFactory {
        public Id FactoryId => ContextTests.FactoryId;

        public IContextEntry ProduceValue() => ProduceValue(ContextEntryState.Empty);

        public IContextEntry ProduceValue(ContextEntryState state) =>
            new CounterEntry(FactoryId, state.Id ?? BaseEntryId / Id.Random());
    }

    private static Context CreateContextWithFactory() {
        return Context.Create(
            new IContextEntry.ServiceContextEntry(
                FactoryId,
                typeof(IStatefulDataContextEntryFactory),
                _ => new CounterFactory()
            )
        );
    }

    [Fact]
    public void ServiceIsLazyAndConstructedOnce() {
        var constructions = 0;
        var context = Context.Create(
            new IContextEntry.ServiceContextEntry(ServiceId, typeof(string), _ => {
                constructions++;
                return "hello";
            })
        );

        Assert.Equal(0, constructions);
        Assert.Equal("hello", context.GetSingle<string>(ServiceId));
        Assert.Equal("hello", context.GetSingle<string>(ServiceId));
        Assert.Equal(1, constructions);
    }

    [Fact]
    public void GetByTypeFindsAssignableEntries() {
        var context = Context.Create(
            new IContextEntry.ServiceContextEntry(ServiceId, typeof(List<int>), _ => new List<int> { 1 })
        );

        var byInterface = context.Get<IEnumerable<int>>();
        Assert.Single(byInterface);
        Assert.Equal(new[] { 1 }, byInterface.Single().Value);
    }

    [Fact]
    public void GetSingleByMissingIdThrows() {
        var context = Context.Create();
        Assert.Throws<KeyNotFoundException>(() => context.GetSingle<string>("no.such.id".AsId()));
    }

    [Fact]
    public void LookupSeesEntriesProducedAfterItWasCreated() {
        var context = CreateContextWithFactory();
        var lookup = context.Lookup<CounterEntry>();
        Assert.Empty(lookup.Get());

        var produced = context.ProduceSingle<CounterEntry>(FactoryId);

        var entries = lookup.Get();
        Assert.Single(entries);
        Assert.Equal(produced.Id, entries.Single().Id);
    }

    [Fact]
    public void ProduceSingleRegistersRetrievableEntry() {
        var context = CreateContextWithFactory();
        var produced = context.ProduceSingle<CounterEntry>(FactoryId);
        Assert.Same(produced, context.GetSingle<CounterEntry>(produced.Id));
    }

    [Fact]
    public void StoreRestoreRoundTripsThroughSaveSerialization() {
        // Mirrors HandleSave/HandleLoad: Store -> JSON string -> Deserialize -> Restore
        // into a fresh context that recreates the entry via its FactoryId.
        var original = CreateContextWithFactory();
        var entry = original.ProduceSingle<CounterEntry>(FactoryId);
        entry.Value = 42;

        var serialized = original.Store().Serialize();
        var deserialized = serialized.Deserialize<Dictionary<Id, Dictionary<string, object>>>();
        Assert.NotNull(deserialized);

        var restored = CreateContextWithFactory();
        restored.Restore(deserialized!);

        var restoredEntry = restored.GetSingle<CounterEntry>(entry.Id);
        Assert.Equal(42, restoredEntry.Value);
        Assert.Equal(entry.Id, restoredEntry.Id);
    }

    [Fact]
    public void ContextEntryStateGetOrDefaultFallsBack() {
        var state = ContextEntryState.Empty;
        Assert.Equal("fallback", state.GetOrDefault("missing", "fallback"));

        state.Set("present", "value");
        Assert.Equal("value", state.GetOrDefault("present", "fallback"));
        Assert.Equal("value", state.Get<string>("present"));
    }

    [Fact]
    public void SetNullThrows() {
        var state = ContextEntryState.Empty;
        Assert.Throws<ArgumentNullException>(() => state.Set<string>("key", null!));
    }
}
