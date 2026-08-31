using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;

namespace Computers.ShippingController.Domain.Wire;

public static class ShippingRequestParser {
    public static ShippingRequest ParseBody(JObject payload) {
        var cmd = payload["cmd"]?.Value<string>();
        switch (cmd) {
            case "ping":
                return new ShippingPingRequest();

            case "list":
                return new ShippingListRequest();

            case "price":
                return new PriceRequest(ReadItemId(payload));

            case "sell": {
                int? count = null;
                if (payload["count"] is not { } token || token.Type == JTokenType.Null) {
                    return new SellRequest(ReadItemId(payload), count);
                }
                
                if (token.Type != JTokenType.Integer || token.Value<int>() < 1) {
                    throw new PeripheralRequestException("count must be a positive integer");
                }
                
                count = token.Value<int>();
                return new SellRequest(ReadItemId(payload), count);
            }

            default:
                throw new PeripheralRequestException($"unknown command '{cmd}'");
        }
    }

    private static string ReadItemId(JObject payload) {
        return payload["itemId"]?.Value<string>()
            ?? throw new PeripheralRequestException("itemId is required");
    }
}
