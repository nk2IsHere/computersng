using Computers.Peripheral.Domain;
using Computers.PlayerSensor.Domain.Wire;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;

namespace Computers.PlayerSensor.Domain;

public class PlayerSensorCommandProcessor {
    private readonly PeripheralSubscriptions _subscriptions;

    public PlayerSensorCommandProcessor(PeripheralSubscriptions subscriptions) {
        _subscriptions = subscriptions;
    }

    public Reply? Process(string sourceAddress, JObject request, SensorReading? reading) {
        if (!WireRequests.TryReadCid(request, out var cid)) {
            return null;
        }

        try {
            if (_subscriptions.TryHandle(request["cmd"]?.Value<string>(), request, sourceAddress)) {
                return Reply.Success(cid, null);
            }

            return Reply.Success(cid, Dispatch(SensorRequestParser.ParseBody(request), reading));
        } catch (PeripheralRequestException exception) {
            return Reply.Failure(cid, exception.Message);
        }
    }

    private static object Dispatch(SensorRequest request, SensorReading? reading) {
        switch (request) {
            case SensorPingRequest:
                return new PingResult("playerSensor");

            case SensorReadRequest:
                return reading ?? throw new PeripheralRequestException("sensor has no world position");

            default:
                throw new PeripheralRequestException($"unknown command '{request.GetType().Name}'");
        }
    }
}
