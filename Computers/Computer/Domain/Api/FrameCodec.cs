using System.IO.Compression;
using System.Text;
using Computers.Computer.Utils;
using Microsoft.Xna.Framework;

namespace Computers.Computer.Domain.Api;

// Packed RGBA form of a color for the frame wire format.
public static class ColorPacking {
    public static uint Pack(this Color color) {
        return ((uint) color.R << 24) | ((uint) color.G << 16) | ((uint) color.B << 8) | color.A;
    }

    public static Color Unpack(this uint packed) {
        return new Color((int) (packed >> 24) & 0xFF, (int) (packed >> 16) & 0xFF, (int) (packed >> 8) & 0xFF, (int) packed & 0xFF);
    }
}

public record RawRun(int Length, uint Color);

// Run length encoded copy of one persistent pixel layer.
public record RawLayer(IReadOnlyList<RawRun> Runs) {
    public static RawLayer From(Color[] pixels) {
        var runs = new List<RawRun>();
        var index = 0;
        while (index < pixels.Length) {
            var color = pixels[index].Pack();
            var length = 1;
            while (index + length < pixels.Length && pixels[index + length].Pack() == color) {
                length++;
            }
            runs.Add(new RawRun(length, color));
            index += length;
        }
        return new RawLayer(runs);
    }

    public void ApplyTo(Color[] target) {
        var index = 0;
        foreach (var run in Runs) {
            var color = run.Color.Unpack();
            for (var i = 0; i < run.Length && index < target.Length; i++) {
                target[index++] = color;
            }
        }
    }
}

public record DecodedFrame(List<IRenderCommand> Commands, RawLayer? Background, RawLayer? Foreground);

// Binary frame serialization. Every render command encodes itself, so this codec
// only owns the framing, the opcode dispatch on decode and the two optional raw
// layers, the whole stream deflated.
public static class FrameCodec {
    private const byte CodecVersion = 1;

    public static byte[] Encode(IReadOnlyList<IRenderCommand> commands, RawLayer? background, RawLayer? foreground) {
        using var buffer = new MemoryStream();
        using (var deflate = new DeflateStream(buffer, CompressionLevel.Fastest, leaveOpen: true))
        using (var writer = new BinaryWriter(deflate, Encoding.UTF8)) {
            writer.Write(CodecVersion);
            writer.Write7BitEncodedInt(commands.Count);
            foreach (var command in commands) {
                command.Encode(writer);
            }
            WriteLayer(writer, background);
            WriteLayer(writer, foreground);
        }
        return buffer.ToArray();
    }

    public static DecodedFrame Decode(byte[] bytes, BmFont font) {
        using var buffer = new MemoryStream(bytes);
        using var deflate = new DeflateStream(buffer, CompressionMode.Decompress);
        using var reader = new BinaryReader(deflate, Encoding.UTF8);

        var version = reader.ReadByte();
        if (version != CodecVersion) {
            throw new InvalidOperationException($"frame codec version {version} is not supported");
        }

        var count = reader.Read7BitEncodedInt();
        var commands = new List<IRenderCommand>(count);
        for (var i = 0; i < count; i++) {
            commands.Add(ReadCommand(reader, font));
        }
        return new DecodedFrame(commands, ReadLayer(reader), ReadLayer(reader));
    }

    private static IRenderCommand ReadCommand(BinaryReader reader, BmFont font) {
        var opcode = reader.ReadByte();
        return opcode switch {
            TextRenderCommand.Opcode => TextRenderCommand.Decode(reader, font),
            RectangleRenderCommand.Opcode => RectangleRenderCommand.Decode(reader),
            BorderRectangleRenderCommand.Opcode => BorderRectangleRenderCommand.Decode(reader),
            CircleRenderCommand.Opcode => CircleRenderCommand.Decode(reader),
            BorderCircleRenderCommand.Opcode => BorderCircleRenderCommand.Decode(reader),
            LineRenderCommand.Opcode => LineRenderCommand.Decode(reader),
            _ => throw new InvalidOperationException($"unknown frame opcode {opcode}")
        };
    }

    private static void WriteLayer(BinaryWriter writer, RawLayer? layer) {
        if (layer is null) {
            writer.Write(false);
            return;
        }
        writer.Write(true);
        writer.Write7BitEncodedInt(layer.Runs.Count);
        foreach (var run in layer.Runs) {
            writer.Write7BitEncodedInt(run.Length);
            writer.Write(run.Color);
        }
    }

    private static RawLayer? ReadLayer(BinaryReader reader) {
        if (!reader.ReadBoolean()) {
            return null;
        }
        var count = reader.Read7BitEncodedInt();
        var runs = new List<RawRun>(count);
        for (var i = 0; i < count; i++) {
            runs.Add(new RawRun(reader.Read7BitEncodedInt(), reader.ReadUInt32()));
        }
        return new RawLayer(runs);
    }
}
