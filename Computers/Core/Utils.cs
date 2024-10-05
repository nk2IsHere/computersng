
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Computers.Core;

public static class Utils {
    public static void ForEach<T>(
        this IEnumerable<T> source,
        Action<T> action
    ) {
        foreach (var element in source)
            action(element);
    }
    
    private static readonly JsonSerializerSettings JsonSerializerSettings = new() {
        TypeNameHandling = TypeNameHandling.All,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        PreserveReferencesHandling = PreserveReferencesHandling.All,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
    };
    
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    
    public static T? Deserialize<T>(this string value) {
        return JsonConvert.DeserializeObject<T>(value, JsonSerializerSettings);
    }
    
    public static string Serialize<T>(this T value) {
        return JsonConvert.SerializeObject(value, JsonSerializerSettings);
    }

    public static T DeserializeConfiguration<T>(this string value) {
        try {
            return YamlDeserializer.Deserialize<T>(value);
        } catch (YamlException e) {
            throw new Exception("Failed to deserialize configuration", e.InnerException);
        }
    }
    
    // From: https://stackoverflow.com/questions/8094867/good-gethashcode-override-for-list-of-foo-objects-respecting-the-order
    public static int GetSequenceHashCode<TItem>(this IEnumerable<TItem>? list)
    {
        if (list == null) return 0;
        const int seedValue = 0x2D2816FE;
        const int primeNumber = 397;
        return list.Aggregate(
            seedValue, 
            (current, item) => current * primeNumber + (
                Equals(item, default(TItem)) 
                    ? 0
                    : item!.GetHashCode()
            )
        );
    }
    
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) {
        return source.Where(x => x != null)!;
    }
}
