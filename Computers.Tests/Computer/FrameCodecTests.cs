using Computers.Computer.Domain.Api;
using Microsoft.Xna.Framework;
using Xunit;

namespace Computers.Tests.Computer;

public class FrameCodecTests {
    // Text commands never serialize their font, so tests decode with a null font
    // the way a client would pass its own local one.
    private static List<IRenderCommand> SampleCommands() {
        return new List<IRenderCommand> {
            new RectangleRenderCommand(0, 0, 452, 256, new Color(0, 0, 0, 255)),
            new TextRenderCommand("hello from the LAN", 4, 4, 9, null!, new Color(255, 255, 255, 255)),
            new LineRenderCommand(0, 20, 452, 20, new Color(0, 255, 0, 255))
        };
    }

    private static RawLayer SampleBackground() {
        return new RawLayer(new[] { new RawRun(115712, 0x00000000) });
    }

    [Fact]
    public void RoundTripsCommandsAndRawLayers() {
        var decoded = FrameCodec.Decode(FrameCodec.Encode(SampleCommands(), SampleBackground(), null), null!);
        Assert.Equal(3, decoded.Commands.Count);
        var text = Assert.IsType<TextRenderCommand>(decoded.Commands[1]);
        Assert.Equal("hello from the LAN", text.Text);
        Assert.Equal(new Color(255, 255, 255, 255), text.Color);
        Assert.NotNull(decoded.Background);
        Assert.Equal(115712, decoded.Background!.Runs[0].Length);
        Assert.Null(decoded.Foreground);
    }

    [Fact]
    public void RoundTripsEveryCommandKind() {
        var color = new Color(0x11, 0x22, 0x33, 0x44);
        var commands = new List<IRenderCommand> {
            new TextRenderCommand("t", 1, 2, 3, null!, color),
            new RectangleRenderCommand(1, 2, 3, 4, color),
            new BorderRectangleRenderCommand(1, 2, 3, 4, 5, color),
            new CircleRenderCommand(1, 2, 3, color),
            new BorderCircleRenderCommand(1, 2, 3, 4, color),
            new LineRenderCommand(1, 2, 3, 4, color)
        };
        var decoded = FrameCodec.Decode(FrameCodec.Encode(commands, null, null), null!);
        Assert.Equal(commands, decoded.Commands);
    }

    [Fact]
    public void EncodedFrameIsSmall() {
        Assert.True(FrameCodec.Encode(SampleCommands(), SampleBackground(), null).Length < 200);
    }

    [Fact]
    public void DecodeRejectsGarbage() {
        Assert.ThrowsAny<Exception>(() => FrameCodec.Decode(new byte[] { 1, 2, 3 }, null!));
    }

    [Fact]
    public void ColorPackingRoundTripsChannels() {
        var color = new Color(255, 0, 0, 255);
        Assert.Equal(0xFF0000FFu, color.Pack());
        Assert.Equal(color, color.Pack().Unpack());
        Assert.Equal(0x0000FF80u, new Color(0, 0, 255, 128).Pack());
    }

    [Fact]
    public void RawLayerRunLengthEncodesAndApplies() {
        var red = new Color(255, 0, 0, 255);
        var blue = new Color(0, 0, 255, 255);
        var pixels = new[] { red, red, blue, blue };

        var layer = RawLayer.From(pixels);
        Assert.Equal(2, layer.Runs.Count);
        Assert.Equal(2, layer.Runs[0].Length);
        Assert.Equal(0xFF0000FFu, layer.Runs[0].Color);

        var target = new Color[4];
        layer.ApplyTo(target);
        Assert.Equal(pixels, target);
    }
}
