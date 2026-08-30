using Computers.MachineController.Domain.Wire;
using Computers.Peripheral.Domain;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;
using RequestParser = Computers.MachineController.Domain.Wire.RequestParser;

namespace Computers.MachineController.Domain;

public class GroupOpException : PeripheralRequestException {
    public GroupOpException(string error) : base(error) {
    }
}

public interface IMachineGroupOps {
    GroupSnapshot ListSnapshot();

    CollectResult Collect(MachinePosition? target);

    InsertResult Insert(InsertRequest request);

    void Rescan();
}

public class MachineControllerCommandProcessor {
    private readonly IMachineGroupOps _ops;
    private readonly PeripheralSubscriptions _subscriptions;

    public MachineControllerCommandProcessor(IMachineGroupOps ops, PeripheralSubscriptions subscriptions) {
        _ops = ops;
        _subscriptions = subscriptions;
    }

    public Reply? Process(string sourceAddress, JObject request) {
        if (!WireRequests.TryReadCid(request, out var cid)) {
            return null;
        }

        try {
            if (_subscriptions.TryHandle(request["cmd"]?.Value<string>(), request, sourceAddress)) {
                return Reply.Success(cid, null);
            }

            var body = RequestParser.ParseBody(request);
            return Reply.Success(cid, Dispatch(body));
        } catch (PeripheralRequestException exception) {
            return Reply.Failure(cid, exception.Message);
        }
    }

    public static MachineReadyEvent BuildMachineReadyEvent(MachineMemberSnapshot machine) {
        return new MachineReadyEvent("machineReady", machine);
    }

    private object? Dispatch(PeripheralRequest request) {
        switch (request) {
            case PingRequest:
                return new PingResult("machineController");

            case ListRequest:
                return _ops.ListSnapshot();

            case RescanRequest:
                _ops.Rescan();
                return null;

            case CollectRequest collect:
                return _ops.Collect(collect.Machine);

            case InsertRequest insert:
                return _ops.Insert(insert);

            default:
                throw new GroupOpException($"unknown command '{request.GetType().Name}'");
        }
    }
}
