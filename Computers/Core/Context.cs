namespace Computers.Core;

public class Context {
    private Context() {
    }

    private readonly Dictionary<string, IContextEntry> _entries = new();
    private readonly Dictionary<string, object?> _cache = new();
    private readonly List<IInvalidatable> _lookups = new();

    public static Context Empty { get; } = new();

    public static Context Create(params IContextEntry[] entries) {
        var context = new Context();
        foreach (var entry in entries) {
            entry.Restore(context, ContextEntryState.Empty);
            context._entries[entry.Id] = entry;
        }

        return context;
    }

    private T _GetOrAddCached<T>(string id, Func<T> factory) {
        if (_cache.TryGetValue(id, out var value) && value is T cachedResult) {
            return cachedResult;
        }

        value = factory();
        _cache[id] = value;
        
        // Invalidate lookups
        foreach (var lookup in _lookups) {
            lookup.Invalidate();
        }
        
        if (value is T result) return result;
        throw new InvalidCastException();
    }

    public ContextEntry<T> Get<T>(string id) {
        if (!_entries.TryGetValue(id, out var entry)) {
            throw new KeyNotFoundException($"Context entry with id '{id}' not found.");
        }

        var value = _GetOrAddCached(id, () => (T)entry.GetValue(this));
        return new ContextEntry<T>(id, value);
    }

    public ISet<ContextEntry<T>> Get<T>() {
        return _entries.Values
            .Where(
                entry => {
                    var baseType = entry.ValueType;
                    var targetType = typeof(T);
                    return baseType == targetType || baseType.IsAssignableTo(targetType);
                }
            )
            .Select(
                entry => new ContextEntry<T>(
                    entry.Id,
                    _GetOrAddCached(entry.Id, () => (T)entry.GetValue(this))
                )
            )
            .ToHashSet();
    }
    
    public ContextLookup<T> Lookup<T>() {
        var lookup = new ContextLookup<T>(Get<T>);
        _lookups.Add(lookup);
        return lookup;
    }

    public T GetSingle<T>() {
        return Get<T>().Single().Value;
    }
    
    public T GetSingle<T>(string id) {
        return Get<T>(id).Value;
    }

    public T Store<T>(IContextEntry.StatefulDataContextEntry<T> entry) {
        _entries[entry.Id] = entry;
        return _GetOrAddCached(entry.Id, () => (T)entry.GetValue(this));
    }
}

public record ContextEntry<T>(string Id, T Value) {
    public static implicit operator ContextEntry<T>((string Id, T Value) tuple) {
        return new ContextEntry<T>(tuple.Id, tuple.Value);
    }
}

public interface IContextEntry {
    public string Id { get; }
    public Type ValueType { get; }
    public ContextEntryType Type { get; }

    public object GetValue(Context context);

    public void Restore(Context context, ContextEntryState state);

    public ContextEntryState Store(Context context);


    public record ServiceContextEntry(
        string Id,
        Type ValueType,
        Func<Context, object> ValueFactory
    ) : IContextEntry {
        public ContextEntryType Type => ContextEntryType.Service;

        private object? _value;

        public object GetValue(Context context) {
            return _value ??= ValueFactory(context);
        }

        public void Restore(Context context, ContextEntryState state) {
        }

        public ContextEntryState Store(Context context) {
            return ContextEntryState.Empty;
        }
    }

    public record StatelessDataContextEntry(
        string Id,
        Type ValueType,
        object Value
    ) : IContextEntry {
        public ContextEntryType Type => ContextEntryType.StatelessData;

        public object GetValue(Context context) {
            return Value;
        }

        public void Restore(Context context, ContextEntryState state) {
        }

        public ContextEntryState Store(Context context) {
            return ContextEntryState.Empty;
        }
    }

    public abstract class StatefulDataContextEntry<T> : IContextEntry {
        public Type ValueType => typeof(T);

        public ContextEntryType Type => ContextEntryType.StatefulData;

        protected StatefulDataContextEntry(string id) {
            Id = id;
        }

        public string Id { get; }

        public abstract object GetValue(Context context);

        public abstract void Restore(Context context, ContextEntryState state);

        public abstract ContextEntryState Store(Context context);
    }
}

public enum ContextEntryType {
    Service,
    StatelessData,
    StatefulData
}

public class ContextEntryState {
    public static ContextEntryState Empty { get; } = new();

    private readonly Dictionary<string, object> _data = new();

    public T Get<T>(string key) => (T)_data[key];

    public void Set<T>(string key, T value) => _data[key] = value ?? throw new ArgumentNullException(nameof(value));
}

internal interface IInvalidatable {
    internal void Invalidate();
}

public class ContextLookup<T> : IInvalidatable {
    private readonly Func<ISet<ContextEntry<T>>> _lookup;
    private ISet<ContextEntry<T>>? _cache;

    public ContextLookup(Func<ISet<ContextEntry<T>>> lookup) {
        _lookup = lookup;
    }

    public ISet<ContextEntry<T>> Get() {
        if (_cache != null) {
            return _cache;
        }
        
        lock (this) {
            return _cache = _lookup();
        }
    }
    
    public T GetSingle() {
        return Get().Single().Value;
    }

    void IInvalidatable.Invalidate() {
        lock (this) {
            _cache = null;
        }
    }
}
