using Computers.Peripheral.Domain;
using Computers.PlayerSensor.Domain.Wire;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;

namespace Computers.PlayerSensor.Domain;

public class PlayerSensorCommandProcessor {
    private readonly PeripheralSubscriptions _subscriptions;
    private readonly Action<int> _configureRadius;

    public PlayerSensorCommandProcessor(PeripheralSubscriptions subscriptions, Action<int> configureRadius) {
        _subscriptions = subscriptions;
        _configureRadius = configureRadius;
    }

    public Reply? Process(string sourceAddress, JObject request, SensorReading? reading, int radius) {
        if (!WireRequests.TryReadCid(request, out var cid)) {
            return null;
        }

        try {
            if (_subscriptions.TryHandle(request["cmd"]?.Value<string>(), request, sourceAddress)) {
                return Reply.Success(cid, null);
            }

            return Reply.Success(cid, Dispatch(SensorRequestParser.ParseBody(request), reading, radius));
        } catch (PeripheralRequestException exception) {
            return Reply.Failure(cid, exception.Message);
        }
    }

    private object? Dispatch(SensorRequest request, SensorReading? reading, int radius) {
        switch (request) {
            case SensorPingRequest:
                return new SensorPingResult("playerSensor", radius);

            case SensorReadRequest:
                return reading ?? throw new PeripheralRequestException("sensor has no world position");

            case SensorConfigureRequest configure:
                _configureRadius(configure.Radius);
                return null;

            default:
                throw new PeripheralRequestException($"unknown command '{request.GetType().Name}'");
        }
    }
}
