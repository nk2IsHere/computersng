using Computers.Multiplayer;
using Computers.Multiplayer.Domain;
using Computers.Multiplayer.Domain.Wire;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.Multiplayer;

public class ScreenCastTests {
    private static FramePayload Payload(string text, RawLayer? background = null) {
        return new FramePayload(new FrameCommand[] { new FrameText(text, 0, 0, 9, 0xFFFFFFFF) }, background, null);
    }

    private static (ScreenCast Cast, FakeTransport Transport) Make(int ticksPerFrame = 1, int maxViewers = 4) {
        var transport = new FakeTransport { LocalPlayerId = 1, IsHost = true, HostPlayerId = 1 };
        var channel = new HostChannel(transport, _ => { });
        return (new ScreenCast(channel, ticksPerFrame, maxViewers, _ => { }), transport);
    }

    private static (JObject Header, FramePayload Payload) Sent(FakeTransport transport, int index) {
        var message = transport.Sent[index].Message;
        return (JObject.Parse(message.Json!), FrameCodec.Decode(message.Bytes!));
    }

    [Fact]
    public void NewViewerGetsASnapshotFrame() {
        var (cast, transport) = Make();
        cast.Subscribe("comp1", 2, 452, 256);
        cast.PublishFrame("comp1", Payload("first"), 0);
        cast.Tick();

        var (header, payload) = Sent(transport, 0);
        Assert.Equal("frame", header["event"]!.Value<string>());
        Assert.True(header["snapshot"]!.Value<bool>());
        Assert.Equal("first", Assert.IsType<FrameText>(payload.Commands[0]).Text);
    }

    [Fact]
    public void ViewerLimitThrowsAReadableError() {
        var (cast, _) = Make(maxViewers: 1);
        cast.Subscribe("comp1", 2, 452, 256);
        var error = Assert.Throws<ChannelRequestException>(() => cast.Subscribe("comp1", 3, 452, 256));
        Assert.Equal("viewer limit reached", error.Message);
    }

    [Fact]
    public void ThrottleSendsOnlyTheNewestPayload() {
        var (cast, transport) = Make(ticksPerFrame: 5);
        cast.Subscribe("comp1", 2, 452, 256);
        cast.Tick();
        transport.Sent.Clear();

        cast.PublishFrame("comp1", Payload("one"), 0);
        cast.PublishFrame("comp1", Payload("two"), 0);
        for (var i = 0; i < 5; i++) {
            cast.Tick();
        }

        var frames = transport.Sent.Select((_, i) => Sent(transport, i).Payload).ToList();
        Assert.Single(frames);
        Assert.Equal("two", Assert.IsType<FrameText>(frames[0].Commands[0]).Text);

        cast.PublishFrame("comp1", Payload("three"), 0);
        cast.Tick();
        Assert.Single(transport.Sent);
    }

    [Fact]
    public void SnapshotKeepsTheLastRawLayers() {
        var (cast, transport) = Make();
        cast.Subscribe("comp1", 2, 452, 256);
        cast.PublishFrame("comp1", Payload("with raw", new RawLayer(new[] { new RawRun(4, 0xFF0000FFu) })), 1);
        cast.Tick();
        cast.PublishFrame("comp1", Payload("without raw"), 1);
        cast.Tick();

        cast.Subscribe("comp1", 3, 452, 256);
        cast.Tick();
        var snapshot = transport.Sent
            .Select((entry, i) => (entry.To, Frame: Sent(transport, i)))
            .Last(sent => sent.To == 3);
        Assert.True(snapshot.Frame.Header["snapshot"]!.Value<bool>());
        Assert.NotNull(snapshot.Frame.Payload.Background);
        Assert.Equal(0xFF0000FFu, snapshot.Frame.Payload.Background!.Runs[0].Color);
    }

    [Fact]
    public void DropPlayerStopsDeliveries() {
        var (cast, transport) = Make();
        cast.Subscribe("comp1", 2, 452, 256);
        cast.Tick();
        transport.Sent.Clear();

        cast.DropPlayer(2);
        cast.PublishFrame("comp1", Payload("gone"), 0);
        cast.Tick();
        Assert.Empty(transport.Sent);
        Assert.False(cast.HasViewers("comp1"));
    }

    [Fact]
    public void DropComputerSendsScreenClosed() {
        var (cast, transport) = Make();
        cast.Subscribe("comp1", 2, 452, 256);
        cast.Tick();
        transport.Sent.Clear();

        cast.DropComputer("comp1");
        var closed = JObject.Parse(Assert.Single(transport.Sent).Message.Json!);
        Assert.Equal("screenClosed", closed["event"]!.Value<string>());
        Assert.Equal("comp1", closed["computerId"]!.Value<string>());
    }

    [Fact]
    public void NoViewersMeansNoTrafficAndNoWatch() {
        var (cast, transport) = Make();
        Assert.False(cast.HasViewers("comp1"));
        cast.PublishFrame("comp1", Payload("nobody"), 0);
        cast.Tick();
        Assert.Empty(transport.Sent);
    }
}
