using Computers.Computer.Domain.Api;
using Microsoft.Xna.Framework;
using Xunit;

namespace Computers.Tests.Computer;

public class FrameComposerTests {
    private static Color[] Fill(int size, Color color) {
        var buffer = new Color[size];
        Array.Fill(buffer, color);
        return buffer;
    }

    [Fact]
    public void BackgroundIsCopiedIntoTarget() {
        var target = new Color[4];
        FrameComposer.Compose(
            target, Fill(4, Color.Blue), new List<IRenderCommand>(), Fill(4, Color.Transparent), 2, 2);
        Assert.All(target, pixel => Assert.Equal(Color.Blue, pixel));
    }

    [Fact]
    public void CommandsDrawOverBackground() {
        var target = new Color[4];
        var commands = new List<IRenderCommand> {
            new RectangleRenderCommand(0, 0, 1, 1, Color.Red) // top-left pixel only
        };
        FrameComposer.Compose(target, Fill(4, Color.Blue), commands, Fill(4, Color.Transparent), 2, 2);
        Assert.Equal(Color.Red, target[0]);
        Assert.Equal(Color.Blue, target[1]);
    }

    [Fact]
    public void OpaqueForegroundWinsOverEverything() {
        var target = new Color[4];
        FrameComposer.Compose(
            target, Fill(4, Color.Blue), new List<IRenderCommand>(), Fill(4, Color.Lime), 2, 2);
        Assert.All(target, pixel => Assert.Equal(Color.Lime, pixel));
    }

    [Fact]
    public void TransparentForegroundLeavesFrameUntouched() {
        var target = new Color[4];
        FrameComposer.Compose(
            target, Fill(4, Color.Blue), new List<IRenderCommand>(), Fill(4, Color.Transparent), 2, 2);
        Assert.All(target, pixel => Assert.Equal(Color.Blue, pixel));
    }
}
