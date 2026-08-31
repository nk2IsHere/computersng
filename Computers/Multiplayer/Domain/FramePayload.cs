using Computers.Computer.Domain.Api;
using Microsoft.Xna.Framework;

namespace Computers.Multiplayer.Domain;

public abstract record FrameCommand;

public record FrameText(string Text, int X, int Y, int Size, uint Color) : FrameCommand;

public record FrameRectangle(int X, int Y, int Width, int Height, uint Color) : FrameCommand;

public record FrameBorderRectangle(int X, int Y, int Width, int Height, int BorderWidth, uint Color) : FrameCommand;

public record FrameCircle(int X, int Y, int Radius, uint Color) : FrameCommand;

public record FrameBorderCircle(int X, int Y, int Radius, int BorderWidth, uint Color) : FrameCommand;

public record FrameLine(int X1, int Y1, int X2, int Y2, uint Color) : FrameCommand;

public record RawRun(int Length, uint Color);

public record RawLayer(IReadOnlyList<RawRun> Runs);

public record FramePayload(IReadOnlyList<FrameCommand> Commands, RawLayer? Background, RawLayer? Foreground);

public static class FramePayloadConverter {
    public static FramePayload Convert(
        IReadOnlyList<IRenderCommand> commands,
        Color[] background,
        Color[] foreground,
        bool includeRaw
    ) {
        var mirrored = new List<FrameCommand>(commands.Count);
        foreach (var command in commands) {
            mirrored.Add(command switch {
                TextRenderCommand text => new FrameText(text.Text, text.X, text.Y, text.Size, Pack(text.Color)),
                RectangleRenderCommand rectangle => new FrameRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, Pack(rectangle.Color)),
                BorderRectangleRenderCommand border => new FrameBorderRectangle(border.X, border.Y, border.Width, border.Height, border.BorderWidth, Pack(border.Color)),
                CircleRenderCommand circle => new FrameCircle(circle.X, circle.Y, circle.Radius, Pack(circle.Color)),
                BorderCircleRenderCommand borderCircle => new FrameBorderCircle(borderCircle.X, borderCircle.Y, borderCircle.Radius, borderCircle.BorderWidth, Pack(borderCircle.Color)),
                LineRenderCommand line => new FrameLine(line.X1, line.Y1, line.X2, line.Y2, Pack(line.Color)),
                _ => throw new InvalidOperationException($"render command {command.GetType().Name} has no frame mirror")
            });
        }

        return new FramePayload(
            mirrored,
            includeRaw ? EncodeLayer(background) : null,
            includeRaw ? EncodeLayer(foreground) : null
        );
    }

    public static List<IRenderCommand> ToRenderCommands(IReadOnlyList<FrameCommand> commands, Computer.Utils.BmFont font) {
        var result = new List<IRenderCommand>(commands.Count);
        foreach (var command in commands) {
            result.Add(command switch {
                FrameText text => new TextRenderCommand(text.Text, text.X, text.Y, text.Size, font, Unpack(text.Color)),
                FrameRectangle rectangle => new RectangleRenderCommand(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, Unpack(rectangle.Color)),
                FrameBorderRectangle border => new BorderRectangleRenderCommand(border.X, border.Y, border.Width, border.Height, border.BorderWidth, Unpack(border.Color)),
                FrameCircle circle => new CircleRenderCommand(circle.X, circle.Y, circle.Radius, Unpack(circle.Color)),
                FrameBorderCircle borderCircle => new BorderCircleRenderCommand(borderCircle.X, borderCircle.Y, borderCircle.Radius, borderCircle.BorderWidth, Unpack(borderCircle.Color)),
                FrameLine line => new LineRenderCommand(line.X1, line.Y1, line.X2, line.Y2, Unpack(line.Color)),
                _ => throw new InvalidOperationException($"frame command {command.GetType().Name} has no render mirror")
            });
        }
        return result;
    }

    public static void DecodeLayer(RawLayer layer, Color[] target) {
        var index = 0;
        foreach (var run in layer.Runs) {
            var color = Unpack(run.Color);
            for (var i = 0; i < run.Length && index < target.Length; i++) {
                target[index++] = color;
            }
        }
    }
    
    private static uint Pack(Color color) {
        return ((uint) color.R << 24) | ((uint) color.G << 16) | ((uint) color.B << 8) | color.A;
    }

    private static Color Unpack(uint packed) {
        return new Color((int) (packed >> 24) & 0xFF, (int) (packed >> 16) & 0xFF, (int) (packed >> 8) & 0xFF, (int) packed & 0xFF);
    }

    private static RawLayer EncodeLayer(Color[] pixels) {
        var runs = new List<RawRun>();
        var index = 0;
        while (index < pixels.Length) {
            var color = Pack(pixels[index]);
            var length = 1;
            while (index + length < pixels.Length && Pack(pixels[index + length]) == color) {
                length++;
            }
            runs.Add(new RawRun(length, color));
            index += length;
        }
        return new RawLayer(runs);
    }
}
