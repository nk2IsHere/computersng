using Computers.MachineController.Domain;
using Computers.MachineController.Domain.Wire;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.Peripheral;

public class CommandProcessorTests {
    private class FakeOps : IMachineGroupOps {
        public List<string> Calls { get; } = new();

        public GroupSnapshot ListSnapshot() {
            Calls.Add("list");
            return new GroupSnapshot(false, new List<GroupMemberSnapshot>());
        }

        public CollectResult Collect(MachinePosition? target) {
            Calls.Add($"collect:{target?.X.ToString() ?? "all"},{target?.Y.ToString() ?? "all"}");
            if (target?.X == 99) {
                throw new GroupOpException("machine not in group");
            }
            return new CollectResult(new List<CollectedItem>());
        }

        public InsertResult Insert(InsertRequest request) {
            Calls.Add(
                $"insert:{request.Machine.X},{request.Machine.Y},{request.ItemId},{request.Count}," +
                $"{request.FromChest?.X.ToString() ?? "-"},{request.FromChest?.Y.ToString() ?? "-"}");
            return new InsertResult(request.Machine, "Copper Ore");
        }

        public void Rescan() {
            Calls.Add("rescan");
        }
    }

    private static (MachineControllerCommandProcessor Processor, FakeOps Ops) Make(int maxSubscribers = 16) {
        var ops = new FakeOps();
        return (new MachineControllerCommandProcessor(ops, maxSubscribers), ops);
    }

    private static Reply Process(MachineControllerCommandProcessor processor, string json, string source = "c1") {
        var reply = processor.Process(source, JObject.Parse(json));
        Assert.NotNull(reply);
        return reply!;
    }

    [Fact]
    public void PingEchoesCidAndReportsKind() {
        var (processor, _) = Make();
        var reply = Process(processor, "{\"cid\":\"abc\",\"cmd\":\"ping\"}");
        Assert.Equal("abc", reply.Re.Value<string>());
        Assert.True(reply.Ok);
        Assert.Equal(new PingResult("machineController"), reply.Data);
    }

    [Fact]
    public void NumericCidIsEchoedAsNumber() {
        var (processor, _) = Make();
        var reply = Process(processor, "{\"cid\":7,\"cmd\":\"ping\"}");
        Assert.Equal(7, reply.Re.Value<int>());
    }

    [Fact]
    public void ListReturnsTheSnapshot() {
        var (processor, ops) = Make();
        var reply = Process(processor, "{\"cid\":\"1\",\"cmd\":\"list\"}");
        Assert.True(reply.Ok);
        Assert.IsType<GroupSnapshot>(reply.Data);
        Assert.Contains("list", ops.Calls);
    }

    [Fact]
    public void CollectAllMapsToNullTarget() {
        var (processor, ops) = Make();
        Process(processor, "{\"cid\":\"1\",\"cmd\":\"collect\",\"machine\":\"all\"}");
        Assert.Contains("collect:all,all", ops.Calls);
    }

    [Fact]
    public void TargetedCollectPassesCoordinates() {
        var (processor, ops) = Make();
        Process(processor, "{\"cid\":\"1\",\"cmd\":\"collect\",\"machine\":{\"x\":3,\"y\":4}}");
        Assert.Contains("collect:3,4", ops.Calls);
    }

    [Fact]
    public void GroupOpExceptionBecomesFailureReply() {
        var (processor, _) = Make();
        var reply = Process(processor, "{\"cid\":\"1\",\"cmd\":\"collect\",\"machine\":{\"x\":99,\"y\":0}}");
        Assert.False(reply.Ok);
        Assert.Equal("machine not in group", reply.Error);
    }

    [Fact]
    public void InsertMapsArgumentsIncludingOptionalChest() {
        var (processor, ops) = Make();
        Process(processor, "{\"cid\":\"1\",\"cmd\":\"insert\",\"machine\":{\"x\":1,\"y\":2},\"itemId\":\"378\",\"count\":2,\"fromChest\":{\"x\":5,\"y\":6}}");
        Assert.Contains("insert:1,2,378,2,5,6", ops.Calls);

        Process(processor, "{\"cid\":\"2\",\"cmd\":\"insert\",\"machine\":{\"x\":1,\"y\":2},\"itemId\":\"378\"}");
        Assert.Contains("insert:1,2,378,1,-,-", ops.Calls); // count defaults to 1, chest optional
    }

    [Fact]
    public void RescanInvokesOps() {
        var (processor, ops) = Make();
        var reply = Process(processor, "{\"cid\":\"1\",\"cmd\":\"rescan\"}");
        Assert.True(reply.Ok);
        Assert.Contains("rescan", ops.Calls);
    }

    [Fact]
    public void UnknownCommandIsAFailureReply() {
        var (processor, _) = Make();
        var reply = Process(processor, "{\"cid\":\"1\",\"cmd\":\"frobnicate\"}");
        Assert.False(reply.Ok);
        Assert.Contains("unknown command", reply.Error);
    }

    [Fact]
    public void PayloadWithoutCidIsDropped() {
        var (processor, _) = Make();
        Assert.Null(processor.Process("c1", JObject.Parse("{\"cmd\":\"ping\"}")));
    }

    [Fact]
    public void SubscribeManagesReadySubscribers() {
        var (processor, _) = Make(maxSubscribers: 2);

        Process(processor, "{\"cid\":\"1\",\"cmd\":\"subscribe\",\"events\":[\"ready\"]}", source: "c1");
        Assert.Contains("c1", processor.ReadySubscribers);

        // duplicate subscribe is idempotent
        Process(processor, "{\"cid\":\"2\",\"cmd\":\"subscribe\",\"events\":[\"ready\"]}", source: "c1");
        Assert.Single(processor.ReadySubscribers);

        Process(processor, "{\"cid\":\"3\",\"cmd\":\"subscribe\",\"events\":[\"ready\"]}", source: "c2");
        var overflow = Process(processor, "{\"cid\":\"4\",\"cmd\":\"subscribe\",\"events\":[\"ready\"]}", source: "c3");
        Assert.False(overflow.Ok);
        Assert.Equal("subscriber limit reached", overflow.Error);

        Process(processor, "{\"cid\":\"5\",\"cmd\":\"unsubscribe\",\"events\":[\"ready\"]}", source: "c1");
        Assert.DoesNotContain("c1", processor.ReadySubscribers);
    }

    // --- Wire-format pins: the serialized bytes must stay identical to the shipped protocol ---

    [Fact]
    public void SuccessReplySerializesToTheFrozenWireFormat() {
        var (processor, _) = Make();
        var reply = Process(processor, "{\"cid\":\"abc\",\"cmd\":\"ping\"}");
        Assert.Equal(
            "{\"re\":\"abc\",\"ok\":true,\"data\":{\"type\":\"machineController\"},\"error\":null}",
            WireJson.Serialize(reply));
    }

    [Fact]
    public void FailureReplySerializesToTheFrozenWireFormat() {
        var (processor, _) = Make();
        var reply = Process(processor, "{\"cid\":9,\"cmd\":\"frobnicate\"}");
        Assert.Equal(
            "{\"re\":9,\"ok\":false,\"data\":null,\"error\":\"unknown command 'frobnicate'\"}",
            WireJson.Serialize(reply));
    }

    [Fact]
    public void MachineReadyEventSerializesToTheFrozenWireFormat() {
        var machine = new MachineMemberSnapshot(1, 2, "13", "Furnace", MachineState.Ready, "Copper Bar", 0);
        var push = MachineControllerCommandProcessor.BuildMachineReadyEvent(machine);
        // Derived record properties serialize before the base x/y. Key order is irrelevant to JS.
        Assert.Equal(
            "{\"event\":\"machineReady\",\"machine\":{\"itemId\":\"13\",\"name\":\"Furnace\",\"state\":\"ready\",\"heldItem\":\"Copper Bar\",\"minutesUntilReady\":0,\"kind\":\"machine\",\"x\":1,\"y\":2}}",
            WireJson.Serialize(push));
    }
}
