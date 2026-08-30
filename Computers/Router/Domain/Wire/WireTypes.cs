using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Computers.Router.Domain.Wire;

public static class WireJson {
    private static readonly JsonSerializerSettings Settings = new() {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = { new StringEnumConverter(new CamelCaseNamingStrategy()) },
        NullValueHandling = NullValueHandling.Include
    };

    public static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);
}

public record Reply(JToken Re, bool Ok, object? Data, string? Error) {
    public static Reply Success(JToken re, object? data) => new(re, true, data, null);
    public static Reply Failure(JToken re, string error) => new(re, false, null, error);
}

public static class WireRequests {
    public static bool TryReadCid(JObject payload, out JToken cid) {
        var token = payload["cid"];
        if (token is null || token.Type == JTokenType.Null) {
            cid = JValue.CreateNull();
            return false;
        }

        cid = token;
        return true;
    }
}

public class RouterRequestException : Exception {
    public RouterRequestException(string error) : base(error) {
    }
}

public abstract record RouterRequest;

public record RouterPingRequest : RouterRequest;

public record RouterDiscoverRequest : RouterRequest;

public record RouterConfigureRequest(int? Channel) : RouterRequest;

public record RouterPingResult(string Type, Computers.Peripheral.PeripheralTier Tier, int? Channel);

public record RouterDiscoverResult(string Router, IReadOnlyList<string> Endpoints);
