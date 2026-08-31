using Computers.Multiplayer.Domain.Wire;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.Multiplayer;

public class ChannelRequestParserTests {
    [Fact]
    public void ParsesOpenScreen() {
        var request = ChannelRequestParser.Parse("openScreen", JObject.Parse("{\"cid\":\"c1\",\"x\":5,\"y\":6,\"location\":\"Farm\"}"));
        var open = Assert.IsType<OpenScreenRequest>(request);
        Assert.Equal(5, open.X);
        Assert.Equal(6, open.Y);
        Assert.Equal("Farm", open.Location);
    }

    [Fact]
    public void LocationDefaultsToFarm() {
        var open = Assert.IsType<OpenScreenRequest>(ChannelRequestParser.Parse("openScreen", JObject.Parse("{\"x\":1,\"y\":2}")));
        Assert.Equal("Farm", open.Location);
    }

    [Fact]
    public void ParsesScreenInputKinds() {
        var key = Assert.IsType<ScreenInputRequest>(ChannelRequestParser.Parse("screenInput", JObject.Parse("{\"computerId\":\"a\",\"kind\":\"key\",\"a\":13,\"b\":0}")));
        Assert.Equal("key", key.Kind);
        Assert.Equal(13, key.A);
        Assert.IsType<ScreenInputRequest>(ChannelRequestParser.Parse("screenInput", JObject.Parse("{\"computerId\":\"a\",\"kind\":\"leftClick\",\"a\":10,\"b\":20}")));
        Assert.IsType<ScreenInputRequest>(ChannelRequestParser.Parse("screenInput", JObject.Parse("{\"computerId\":\"a\",\"kind\":\"rightClick\",\"a\":10,\"b\":20}")));
        Assert.IsType<ScreenInputRequest>(ChannelRequestParser.Parse("screenInput", JObject.Parse("{\"computerId\":\"a\",\"kind\":\"wheel\",\"a\":-1,\"b\":0}")));
        Assert.Throws<ChannelRequestException>(() => ChannelRequestParser.Parse("screenInput", JObject.Parse("{\"computerId\":\"a\",\"kind\":\"dance\",\"a\":0,\"b\":0}")));
    }

    [Fact]
    public void ParsesLifecycleCommands() {
        Assert.IsType<CloseScreenRequest>(ChannelRequestParser.Parse("closeScreen", JObject.Parse("{\"computerId\":\"a\"}")));
        Assert.IsType<InitializeComputerRequest>(ChannelRequestParser.Parse("initializeComputer", JObject.Parse("{\"x\":1,\"y\":2,\"location\":\"Farm\"}")));
    }

    [Fact]
    public void RejectsUnknownCommand() {
        var error = Assert.Throws<ChannelRequestException>(() => ChannelRequestParser.Parse("nope", new JObject()));
        Assert.Equal("unknown command 'nope'", error.Message);
    }
}
