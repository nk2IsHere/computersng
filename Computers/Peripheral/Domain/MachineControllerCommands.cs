using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;

namespace Computers.Peripheral.Domain;

public class GroupOpException : Exception {
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
    private readonly int _maxSubscribers;
    private readonly HashSet<string> _readySubscribers = new();

    public MachineControllerCommandProcessor(IMachineGroupOps ops, int maxSubscribers) {
        _ops = ops;
        _maxSubscribers = maxSubscribers;
    }

    public IReadOnlyCollection<string> ReadySubscribers => _readySubscribers;

    public Reply? Process(string sourceAddress, JObject request) {
        if (!WireRequests.TryReadCid(request, out var cid)) {
            return null;
        }

        try {
            var body = RequestParser.ParseBody(request);
            return Reply.Success(cid, Dispatch(sourceAddress, body));
        } catch (GroupOpException exception) {
            return Reply.Failure(cid, exception.Message);
        }
    }

    public static MachineReadyEvent BuildMachineReadyEvent(MachineSnapshot machine) {
        return new MachineReadyEvent("machineReady", machine);
    }

    private object? Dispatch(string sourceAddress, PeripheralRequest request) {
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

            case SubscribeRequest:
                if (!_readySubscribers.Contains(sourceAddress) && _readySubscribers.Count >= _maxSubscribers) {
                    throw new GroupOpException("subscriber limit reached");
                }
                _readySubscribers.Add(sourceAddress);
                return null;

            case UnsubscribeRequest:
                _readySubscribers.Remove(sourceAddress);
                return null;

            default:
                throw new GroupOpException($"unknown command '{request.GetType().Name}'");
        }
    }
}
