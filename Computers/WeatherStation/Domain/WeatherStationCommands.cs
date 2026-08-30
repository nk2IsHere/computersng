using Computers.Peripheral.Domain;
using Computers.Router.Domain.Wire;
using Computers.WeatherStation.Domain.Wire;
using Newtonsoft.Json.Linq;

namespace Computers.WeatherStation.Domain;

public class WeatherStationCommandProcessor {
    private readonly PeripheralSubscriptions _subscriptions;

    public WeatherStationCommandProcessor(PeripheralSubscriptions subscriptions) {
        _subscriptions = subscriptions;
    }

    public Reply? Process(string sourceAddress, JObject request, WeatherReading? reading) {
        if (!WireRequests.TryReadCid(request, out var cid)) {
            return null;
        }

        try {
            if (_subscriptions.TryHandle(request["cmd"]?.Value<string>(), request, sourceAddress)) {
                return Reply.Success(cid, null);
            }

            return Reply.Success(cid, Dispatch(WeatherRequestParser.ParseBody(request), reading));
        } catch (PeripheralRequestException exception) {
            return Reply.Failure(cid, exception.Message);
        }
    }

    private static object Dispatch(WeatherRequest request, WeatherReading? reading) {
        switch (request) {
            case WeatherPingRequest:
                return new PingResult("weatherStation");

            case WeatherReadRequest:
                return reading ?? throw new PeripheralRequestException("station has no world position");

            default:
                throw new PeripheralRequestException($"unknown command '{request.GetType().Name}'");
        }
    }
}
