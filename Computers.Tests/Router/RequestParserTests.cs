using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.Router;

public class RequestParserTests {
    [Fact]
    public void ParsesPing() {
        Assert.IsType<RouterPingRequest>(RequestParser.ParseBody(JObject.Parse("{\"cid\":1,\"cmd\":\"ping\"}")));
    }

    [Fact]
    public void ParsesDiscover() {
        Assert.IsType<RouterDiscoverRequest>(RequestParser.ParseBody(JObject.Parse("{\"cid\":1,\"cmd\":\"discover\"}")));
    }

    [Fact]
    public void ParsesConfigureWithChannel() {
        var request = Assert.IsType<RouterConfigureRequest>(
            RequestParser.ParseBody(JObject.Parse("{\"cid\":1,\"cmd\":\"configure\",\"channel\":7}")));
        Assert.Equal(7, request.Channel);
    }

    [Fact]
    public void ParsesConfigureWithNullChannel() {
        var request = Assert.IsType<RouterConfigureRequest>(
            RequestParser.ParseBody(JObject.Parse("{\"cid\":1,\"cmd\":\"configure\",\"channel\":null}")));
        Assert.Null(request.Channel);
    }

    [Fact]
    public void ConfigureWithoutChannelKeyThrows() {
        var e = Assert.Throws<RouterRequestException>(
            () => RequestParser.ParseBody(JObject.Parse("{\"cid\":1,\"cmd\":\"configure\"}")));
        Assert.Contains("channel", e.Message);
    }

    [Fact]
    public void ConfigureWithNonIntegerChannelThrows() {
        Assert.Throws<RouterRequestException>(
            () => RequestParser.ParseBody(JObject.Parse("{\"cid\":1,\"cmd\":\"configure\",\"channel\":\"five\"}")));
    }

    [Fact]
    public void UnknownCommandThrowsWithCommandName() {
        var e = Assert.Throws<RouterRequestException>(
            () => RequestParser.ParseBody(JObject.Parse("{\"cid\":1,\"cmd\":\"reboot\"}")));
        Assert.Contains("reboot", e.Message);
    }

    [Fact]
    public void TryReadCidRejectsMissingAndNull() {
        Assert.False(WireRequests.TryReadCid(JObject.Parse("{\"cmd\":\"ping\"}"), out _));
        Assert.False(WireRequests.TryReadCid(JObject.Parse("{\"cid\":null,\"cmd\":\"ping\"}"), out _));
        Assert.True(WireRequests.TryReadCid(JObject.Parse("{\"cid\":\"c1\"}"), out var cid));
        Assert.Equal("c1", cid.Value<string>());
        Assert.True(WireRequests.TryReadCid(JObject.Parse("{\"cid\":7}"), out var numericCid));
        Assert.Equal(7, numericCid.Value<int>());
    }
}
