using Computers.Peripheral;
using Computers.Peripheral.Domain;
using Computers.PlayerSensor.Domain.Wire;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;

namespace Computers.PlayerSensor.Domain;

public class PlayerSensorCommandProcessor {
    private readonly PeripheralSubscriptions _subscriptions;
    private readonly Action<int> _configureRadius;
    private readonly PeripheralTier _tier;

    public PlayerSensorCommandProcessor(PeripheralSubscriptions subscriptions, Action<int> configureRadius, PeripheralTier tier) {
        _subscriptions = subscriptions;
        _configureRadius = configureRadius;
        _tier = tier;
    }

    public Reply? Process(string sourceAddress, JObject request, SensorReading? reading, int radius) {
        if (!WireRequests.TryReadCid(request, out var cid)) {
            return null;
        }

        try
        {
            return Reply.Success(
                cid,
                _subscriptions.TryHandle(request["cmd"]?.Value<string>(), request, sourceAddress) 
                    ? null 
                    : Dispatch(SensorRequestParser.ParseBody(request), reading, radius)
            );
        } catch (PeripheralRequestException exception) {
            return Reply.Failure(cid, exception.Message);
        }
    }

    private object? Dispatch(SensorRequest request, SensorReading? reading, int radius) {
        switch (request) {
            case SensorPingRequest:
                return new SensorPingResult("playerSensor", _tier, radius);

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
