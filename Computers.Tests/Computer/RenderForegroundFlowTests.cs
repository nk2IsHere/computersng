using Computers.Computer;
using Computers.Computer.Domain.Api;
using Computers.Computer.Utils;
using Jint;
using Microsoft.Xna.Framework;
using Xunit;

namespace Computers.Tests.Computer;

/// <summary>
/// End-to-end reproduction of the in-game SetForeground flow: JS (via Jint) writes a
/// foreground pixel between frames, the OS loop draws a full-screen rectangle, and the
/// compose-on-End path publishes through the triple buffer. Mirrors RenderComputerApi's
/// wiring minus the GPU texture.
/// </summary>
public class RenderForegroundFlowTests {
    private const int Width = 8;
    private const int Height = 8;

    private static Configuration TestConfiguration() => new() {
        Render = new RenderConfiguration {
            CanvasWidth = Width,
            CanvasHeight = Height,
            FontDefaultScale = 1
        }
    };

    [Fact]
    public void ForegroundPixelSetBetweenFramesSurvivesBeginAndAppearsInPublishedFrame() {
        var configuration = TestConfiguration();
        var pixelCount = Width * Height;
        var frames = new TripleBuffer<Color[]>(new Color[pixelCount], new Color[pixelCount], new Color[pixelCount]);

        var state = new RenderComputerState(
            configuration,
            null!, // font unused by SetForeground/Rectangle/Begin/End
            () => { },
            (commands, background, foreground) => {
                FrameComposer.Compose(frames.ProduceSlot, background, commands, foreground, Width, Height);
                frames.Publish();
            }
        );

        var engine = new Engine();
        engine.SetValue("Render", state);

        // Frame 1: normal OS frame.
        engine.Execute("Render.Begin()");
        engine.Execute("Render.Rectangle(0, 0, 8, 8, [0, 0, 0, 255])");
        engine.Execute("Render.End()");

        // Console eval between frames (this is what the user types).
        engine.Execute("Render.SetForeground(2, 3, [0, 255, 0, 255])");

        // Frame 2: the next OS frame must show the green pixel.
        engine.Execute("Render.Begin()");
        engine.Execute("Render.Rectangle(0, 0, 8, 8, [0, 0, 0, 255])");
        engine.Execute("Render.End()");

        var (frame, isNew) = frames.Consume();
        Assert.True(isNew);
        Assert.Equal(new Color(0, 255, 0, 255), frame[3 * Width + 2]);
        Assert.Equal(new Color(0, 0, 0, 255), frame[0]); // rest is the black rectangle
    }
}
