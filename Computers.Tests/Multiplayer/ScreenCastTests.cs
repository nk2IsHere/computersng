using Computers.Computer.Domain.Api;
using Computers.Multiplayer.Domain;
using Computers.Multiplayer.Domain.Wire;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.Multiplayer;

public class ScreenCastTests {
    private static readonly Color[] NoPixels = Array.Empty<Color>();

    private static List<IRenderCommand> Commands(string text) {
        return new List<IRenderCommand> {
            new TextRenderCommand(text, 0, 0, 9, null!, new Color(255, 255, 255, 255))
        };
    }

    private static (ScreenCast Cast, FakeTransport Transport) Make(int ticksPerFrame = 1, int maxViewers = 4) {
        var transport = new FakeTransport { LocalPlayerId = 1, IsHost = true, HostPlayerId = 1 };
        var channel = new HostChannel(transport, _ => { });
        return (new ScreenCast(channel, ticksPerFrame, maxViewers, _ => { }), transport);
    }

    private static (JObject Header, DecodedFrame Frame) Sent(FakeTransport transport, int index) {
        var message = transport.Sent[index].Message;
        return (JObject.Parse(message.Json!), FrameCodec.Decode(message.Bytes!, null!));
    }

    [Fact]
    public void NewViewerGetsASnapshotFrame() {
        var (cast, transport) = Make();
        cast.Subscribe("comp1", 2, 452, 256);
        cast.OnFrame("comp1", Commands("first"), NoPixels, NoPixels, 0);
        cast.Tick();

        var (header, frame) = Sent(transport, 0);
        Assert.Equal("frame", header["event"]!.Value<string>());
        Assert.True(header["snapshot"]!.Value<bool>());
        Assert.Equal("first", Assert.IsType<TextRenderCommand>(frame.Commands[0]).Text);
    }

    [Fact]
    public void ViewerLimitThrowsAReadableError() {
        var (cast, _) = Make(maxViewers: 1);
        cast.Subscribe("comp1", 2, 452, 256);
        var error = Assert.Throws<ChannelRequestException>(() => cast.Subscribe("comp1", 3, 452, 256));
        Assert.Equal("viewer limit reached", error.Message);
    }

    [Fact]
    public void ThrottleSendsOnlyTheNewestFrame() {
        var (cast, transport) = Make(ticksPerFrame: 5);
        cast.Subscribe("comp1", 2, 452, 256);
        cast.Tick();
        transport.Sent.Clear();

        cast.OnFrame("comp1", Commands("one"), NoPixels, NoPixels, 0);
        cast.OnFrame("comp1", Commands("two"), NoPixels, NoPixels, 0);
        for (var i = 0; i < 5; i++) {
            cast.Tick();
        }

        var frames = transport.Sent.Select((_, i) => Sent(transport, i).Frame).ToList();
        Assert.Single(frames);
        Assert.Equal("two", Assert.IsType<TextRenderCommand>(frames[0].Commands[0]).Text);

        cast.OnFrame("comp1", Commands("three"), NoPixels, NoPixels, 0);
        cast.Tick();
        Assert.Single(transport.Sent);
    }

    [Fact]
    public void RawLayersTravelOnlyWhenTheirVersionMoves() {
        var red = new Color(255, 0, 0, 255);
        var pixels = new[] { red, red, red, red };
        var (cast, transport) = Make();
        cast.Subscribe("comp1", 2, 452, 256);
        cast.Tick();
        transport.Sent.Clear();

        cast.OnFrame("comp1", Commands("with raw"), pixels, pixels, 1);
        cast.Tick();
        cast.OnFrame("comp1", Commands("same version"), pixels, pixels, 1);
        cast.Tick();

        Assert.NotNull(Sent(transport, 0).Frame.Background);
        Assert.Null(Sent(transport, 1).Frame.Background);
    }

    [Fact]
    public void SnapshotKeepsTheLastRawLayers() {
        var red = new Color(255, 0, 0, 255);
        var pixels = new[] { red, red, red, red };
        var (cast, transport) = Make();
        cast.Subscribe("comp1", 2, 452, 256);
        cast.OnFrame("comp1", Commands("with raw"), pixels, pixels, 1);
        cast.Tick();
        cast.OnFrame("comp1", Commands("without raw"), pixels, pixels, 1);
        cast.Tick();

        cast.Subscribe("comp1", 3, 452, 256);
        cast.Tick();
        var snapshot = transport.Sent
            .Select((entry, i) => (entry.To, Frame: Sent(transport, i)))
            .Last(sent => sent.To == 3);
        Assert.True(snapshot.Frame.Header["snapshot"]!.Value<bool>());
        Assert.NotNull(snapshot.Frame.Frame.Background);
        Assert.Equal(0xFF0000FFu, snapshot.Frame.Frame.Background!.Runs[0].Color);
    }

    [Fact]
    public void DropPlayerStopsDeliveries() {
        var (cast, transport) = Make();
        cast.Subscribe("comp1", 2, 452, 256);
        cast.Tick();
        transport.Sent.Clear();

        cast.DropPlayer(2);
        cast.OnFrame("comp1", Commands("gone"), NoPixels, NoPixels, 0);
        cast.Tick();
        Assert.Empty(transport.Sent);
        Assert.False(cast.WantsFrames("comp1"));
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
        Assert.False(cast.WantsFrames("comp1"));
        cast.OnFrame("comp1", Commands("nobody"), NoPixels, NoPixels, 0);
        cast.Tick();
        Assert.Empty(transport.Sent);
    }
}
