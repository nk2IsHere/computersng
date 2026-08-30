using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;

namespace Computers.PlayerSensor.Domain.Wire;

public static class SensorRequestParser {
    public static SensorRequest ParseBody(JObject payload) {
        var cmd = payload["cmd"]?.Value<string>();
        switch (cmd) {
            case "ping":
                return new SensorPingRequest();

            case "read":
                return new SensorReadRequest();

            case "configure": {
                var token = payload["radius"];
                if (token is null || token.Type != JTokenType.Integer) {
                    throw new PeripheralRequestException("configure requires an integer radius");
                }
                var radius = token.Value<int>();
                return radius < 1
                    ? throw new PeripheralRequestException("radius must be at least 1")
                    : new SensorConfigureRequest(radius);
            }

            default:
                throw new PeripheralRequestException($"unknown command '{cmd}'");
        }
    }
}
