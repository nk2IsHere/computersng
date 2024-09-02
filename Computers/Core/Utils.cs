
using Newtonsoft.Json;

namespace Computers.Core;

public static class Utils {
    public static void ForEach<T>(
        this IEnumerable<T> source,
        Action<T> action
    ) {
        foreach (var element in source)
            action(element);
    }
    
    private static readonly JsonSerializerSettings _serializerSettings = new() {
        TypeNameHandling = TypeNameHandling.All,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        PreserveReferencesHandling = PreserveReferencesHandling.All,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
    };
    
    public static T? Deserialize<T>(this string value) {
        return JsonConvert.DeserializeObject<T>(value, _serializerSettings);
    }
    
    public static string Serialize<T>(this T value) {
        return JsonConvert.SerializeObject(value, _serializerSettings);
    }
}
