using Newtonsoft.Json.Linq;

namespace Computers.E2e.Wire;

public static class RequestParser {
    public static E2eRequest Parse(string cmd, JObject payload) {
        return cmd switch {
            "status" => new StatusRequest(),
            "place" => new PlaceRequest(RequireString(payload, "item"), RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload)),
            "placeChest" => new PlaceChestRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload), ReadChestItems(payload)),
            "remove" => new RemoveRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload)),
            "writeDisk" => new WriteDiskRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload), RequireString(payload, "path"), RequireString(payload, "content")),
            "readDisk" => new ReadDiskRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload), RequireString(payload, "path")),
            "restartComputer" => new RestartComputerRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload)),
            "interact" => new InteractRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload)),
            "queryObject" => new QueryObjectRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload)),
            "waitTicks" => new WaitTicksRequest(RequirePositiveInt(payload, "count")),
            "queryShippingBin" => new QueryShippingBinRequest(),
            "queryFarmhouse" => new QueryFarmhouseRequest(),
            "queryActiveMenu" => new QueryActiveMenuRequest(),
            "menuKey" => new MenuKeyRequest(RequireInt(payload, "key")),
            "menuClick" => new MenuClickRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), RequireString(payload, "button")),
            "startSplitScreen" => new StartSplitScreenRequest(),
            "insertDisk" => new InsertDiskRequest(RequireInt(payload, "x"), RequireInt(payload, "y"), ReadLocation(payload)),
            "saveGame" => new SaveGameRequest(),
            "quit" => new QuitRequest(),
            _ => throw new E2eRequestException($"unknown command '{cmd}'")
        };
    }

    private static string ReadLocation(JObject payload) {
        var token = payload["location"];
        return token is null || token.Type == JTokenType.Null ? "Farm" : token.Value<string>()!;
    }

    private static string RequireString(JObject payload, string key) {
        var token = payload[key];
        if (token is null || token.Type != JTokenType.String) {
            throw new E2eRequestException($"'{key}' must be a string");
        }
        return token.Value<string>()!;
    }

    private static int RequireInt(JObject payload, string key) {
        var token = payload[key];
        if (token is null || token.Type != JTokenType.Integer) {
            throw new E2eRequestException($"'{key}' must be an integer");
        }
        return token.Value<int>();
    }

    private static int RequirePositiveInt(JObject payload, string key) {
        var value = RequireInt(payload, key);
        if (value < 1) {
            throw new E2eRequestException($"'{key}' must be at least 1");
        }
        return value;
    }

    private static IReadOnlyList<ChestItem> ReadChestItems(JObject payload) {
        if (payload["items"] is not JArray items) {
            throw new E2eRequestException("'items' must be a list");
        }
        return items
            .Select(entry => {
                if (entry is not JObject item) {
                    throw new E2eRequestException("each item must be an object");
                }
                var count = item["count"];
                return new ChestItem(
                    RequireString(item, "itemId"),
                    count is null || count.Type == JTokenType.Null ? 1 : count.Value<int>()
                );
            })
            .ToList();
    }
}
