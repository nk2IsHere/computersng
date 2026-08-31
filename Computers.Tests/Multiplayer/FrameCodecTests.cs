using Computers.Computer.Domain.Api;
using Computers.Multiplayer.Domain;
using Microsoft.Xna.Framework;
using Xunit;

namespace Computers.Tests.Multiplayer;

public class FrameCodecTests {
    private static FramePayload Sample() {
        return new FramePayload(
            new FrameCommand[] {
                new FrameRectangle(0, 0, 452, 256, 0x000000FF),
                new FrameText("hello from the LAN", 4, 4, 9, 0xFFFFFFFF),
                new FrameLine(0, 20, 452, 20, 0x00FF00FF)
            },
            new RawLayer(new[] { new RawRun(115712, 0x00000000) }),
            null
        );
    }

    [Fact]
    public void RoundTripsCommandsAndRawLayers() {
        var decoded = FrameCodec.Decode(FrameCodec.Encode(Sample()));
        Assert.Equal(3, decoded.Commands.Count);
        var text = Assert.IsType<FrameText>(decoded.Commands[1]);
        Assert.Equal("hello from the LAN", text.Text);
        Assert.Equal(0xFFFFFFFF, text.Color);
        Assert.NotNull(decoded.Background);
        Assert.Equal(115712, decoded.Background!.Runs[0].Length);
        Assert.Null(decoded.Foreground);
    }

    [Fact]
    public void RoundTripsEveryCommandKind() {
        var payload = new FramePayload(
            new FrameCommand[] {
                new FrameText("t", 1, 2, 3, 0x11223344),
                new FrameRectangle(1, 2, 3, 4, 0x11223344),
                new FrameBorderRectangle(1, 2, 3, 4, 5, 0x11223344),
                new FrameCircle(1, 2, 3, 0x11223344),
                new FrameBorderCircle(1, 2, 3, 4, 0x11223344),
                new FrameLine(1, 2, 3, 4, 0x11223344)
            },
            null,
            null
        );
        var decoded = FrameCodec.Decode(FrameCodec.Encode(payload));
        Assert.Equal(payload.Commands, decoded.Commands);
    }

    [Fact]
    public void EncodedFrameIsSmall() {
        Assert.True(FrameCodec.Encode(Sample()).Length < 200);
    }

    [Fact]
    public void DecodeRejectsGarbage() {
        Assert.ThrowsAny<Exception>(() => FrameCodec.Decode(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void ConverterMirrorsCommandsAndPacksColors() {
        var commands = new List<IRenderCommand> {
            new TextRenderCommand("hi", 3, 4, 9, null!, new Color(255, 0, 0, 255)),
            new RectangleRenderCommand(1, 2, 10, 20, new Color(0, 0, 255, 128))
        };
        var payload = FramePayloadConverter.Convert(commands, Array.Empty<Color>(), Array.Empty<Color>(), includeRaw: false);
        var text = Assert.IsType<FrameText>(payload.Commands[0]);
        Assert.Equal("hi", text.Text);
        Assert.Equal(0xFF0000FFu, text.Color);
        var rectangle = Assert.IsType<FrameRectangle>(payload.Commands[1]);
        Assert.Equal(0x0000FF80u, rectangle.Color);
        Assert.Null(payload.Background);
        Assert.Null(payload.Foreground);
    }

    [Fact]
    public void ConverterRunLengthEncodesRawLayers() {
        var red = new Color(255, 0, 0, 255);
        var blue = new Color(0, 0, 255, 255);
        var pixels = new[] { red, red, blue, blue };
        var payload = FramePayloadConverter.Convert(new List<IRenderCommand>(), pixels, pixels, includeRaw: true);
        Assert.Equal(2, payload.Background!.Runs.Count);
        Assert.Equal(2, payload.Background.Runs[0].Length);
        Assert.Equal(0xFF0000FFu, payload.Background.Runs[0].Color);

        var target = new Color[4];
        FramePayloadConverter.DecodeLayer(payload.Background, target);
        Assert.Equal(pixels, target);
    }
}
