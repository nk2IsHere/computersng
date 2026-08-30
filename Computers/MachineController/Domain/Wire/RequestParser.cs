using Newtonsoft.Json.Linq;

namespace Computers.MachineController.Domain.Wire;

public static class RequestParser {
    public static PeripheralRequest ParseBody(JObject payload) {
        var cmd = payload["cmd"]?.Value<string>();
        switch (cmd) {
            case "ping":
                return new PingRequest();

            case "list":
                return new ListRequest();

            case "rescan":
                return new RescanRequest();

            case "collect": {
                var machine = payload["machine"];
                if (machine is null || (machine.Type == JTokenType.String && machine.Value<string>() == "all")) {
                    return new CollectRequest(null);
                }
                return new CollectRequest(ReadPoint(machine, "machine"));
            }

            case "insert": {
                var machine = ReadPoint(payload["machine"], "machine");
                var itemId = payload["itemId"]?.Value<string>()
                    ?? throw new GroupOpException("insert requires itemId");
                var count = payload["count"]?.Value<int>() ?? 1;

                MachinePosition? fromChest = null;
                if (payload["fromChest"] is { } fromChestToken && fromChestToken.Type != JTokenType.Null) {
                    fromChest = ReadPoint(fromChestToken, "fromChest");
                }

                return new InsertRequest(machine, itemId, count, fromChest);
            }

            default:
                throw new GroupOpException($"unknown command '{cmd}'");
        }
    }

    private static MachinePosition ReadPoint(JToken? token, string name) {
        if (token is null || token.Type != JTokenType.Object) {
            throw new GroupOpException($"{name} requires an {{x, y}} object");
        }

        var x = token["x"]?.Value<int?>();
        var y = token["y"]?.Value<int?>();
        if (x is null || y is null) {
            throw new GroupOpException($"{name} requires an {{x, y}} object");
        }

        return new MachinePosition(x.Value, y.Value);
    }
}
