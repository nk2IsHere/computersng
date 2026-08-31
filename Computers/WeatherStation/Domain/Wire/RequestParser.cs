using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;

namespace Computers.WeatherStation.Domain.Wire;

public static class WeatherRequestParser {
    public static WeatherRequest ParseBody(JObject payload) {
        var cmd = payload["cmd"]?.Value<string>();
        switch (cmd) {
            case "ping":
                return new WeatherPingRequest();

            case "read":
                return new WeatherReadRequest();

            default:
                throw new PeripheralRequestException($"unknown command '{cmd}'");
        }
    }
}
