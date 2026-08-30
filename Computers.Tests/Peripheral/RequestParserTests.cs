using Computers.MachineController.Domain;
using Computers.MachineController.Domain.Wire;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.Peripheral;

public class RequestParserTests {
    private static JObject Payload(string json) => JObject.Parse(json);

    [Fact]
    public void SimpleCommandsParse() {
        Assert.IsType<PingRequest>(RequestParser.ParseBody(Payload("{\"cmd\":\"ping\"}")));
        Assert.IsType<ListRequest>(RequestParser.ParseBody(Payload("{\"cmd\":\"list\"}")));
        Assert.IsType<RescanRequest>(RequestParser.ParseBody(Payload("{\"cmd\":\"rescan\"}")));
    }

    [Fact]
    public void CollectAllVariants() {
        var omitted = Assert.IsType<CollectRequest>(RequestParser.ParseBody(Payload("{\"cmd\":\"collect\"}")));
        Assert.Null(omitted.Machine);

        var explicitAll = Assert.IsType<CollectRequest>(
            RequestParser.ParseBody(Payload("{\"cmd\":\"collect\",\"machine\":\"all\"}")));
        Assert.Null(explicitAll.Machine);
    }

    [Fact]
    public void TargetedCollectParsesCoordinates() {
        var request = Assert.IsType<CollectRequest>(
            RequestParser.ParseBody(Payload("{\"cmd\":\"collect\",\"machine\":{\"x\":3,\"y\":4}}")));
        Assert.Equal(new MachinePosition(3, 4), request.Machine);
    }

    [Fact]
    public void InsertParsesAllArguments() {
        var full = Assert.IsType<InsertRequest>(RequestParser.ParseBody(
            Payload("{\"cmd\":\"insert\",\"machine\":{\"x\":1,\"y\":2},\"itemId\":\"378\",\"count\":2,\"fromChest\":{\"x\":5,\"y\":6}}")));
        Assert.Equal(new MachinePosition(1, 2), full.Machine);
        Assert.Equal("378", full.ItemId);
        Assert.Equal(2, full.Count);
        Assert.Equal(new MachinePosition(5, 6), full.FromChest);

        var minimal = Assert.IsType<InsertRequest>(RequestParser.ParseBody(
            Payload("{\"cmd\":\"insert\",\"machine\":{\"x\":1,\"y\":2},\"itemId\":\"378\"}")));
        Assert.Equal(1, minimal.Count);
        Assert.Null(minimal.FromChest);
    }

    [Fact]
    public void InsertWithoutItemIdThrows() {
        var exception = Assert.Throws<GroupOpException>(
            () => RequestParser.ParseBody(Payload("{\"cmd\":\"insert\",\"machine\":{\"x\":1,\"y\":2}}")));
        Assert.Equal("insert requires itemId", exception.Message);
    }

    [Fact]
    public void MalformedMachineThrowsWithExactMessage() {
        var exception = Assert.Throws<GroupOpException>(
            () => RequestParser.ParseBody(Payload("{\"cmd\":\"insert\",\"machine\":\"nope\",\"itemId\":\"378\"}")));
        Assert.Equal("machine requires an {x, y} object", exception.Message);
    }

    [Fact]
    public void SubscribeParsesEvents() {
        var request = Assert.IsType<SubscribeRequest>(
            RequestParser.ParseBody(Payload("{\"cmd\":\"subscribe\",\"events\":[\"ready\"]}")));
        Assert.Equal(new[] { "ready" }, request.Events);

        Assert.IsType<UnsubscribeRequest>(
            RequestParser.ParseBody(Payload("{\"cmd\":\"unsubscribe\",\"events\":[\"ready\"]}")));
    }

    [Fact]
    public void SubscribeWithoutReadyThrows() {
        var exception = Assert.Throws<GroupOpException>(
            () => RequestParser.ParseBody(Payload("{\"cmd\":\"subscribe\",\"events\":[\"other\"]}")));
        Assert.Equal("only the 'ready' event is supported", exception.Message);
    }

    [Fact]
    public void UnknownCommandThrowsWithExactMessage() {
        var exception = Assert.Throws<GroupOpException>(
            () => RequestParser.ParseBody(Payload("{\"cmd\":\"frobnicate\"}")));
        Assert.Equal("unknown command 'frobnicate'", exception.Message);
    }
}
