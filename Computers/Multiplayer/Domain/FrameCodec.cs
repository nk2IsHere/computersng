using System.IO.Compression;
using System.Text;

namespace Computers.Multiplayer.Domain;

// Binary frame serialization. One opcode byte per command with 7 bit encoded integers,
// then the two optional raw layers, the whole stream deflated.
public static class FrameCodec {
    private const byte CodecVersion = 1;

    private const byte OpText = 1;
    private const byte OpRectangle = 2;
    private const byte OpBorderRectangle = 3;
    private const byte OpCircle = 4;
    private const byte OpBorderCircle = 5;
    private const byte OpLine = 6;

    public static byte[] Encode(FramePayload payload) {
        using var buffer = new MemoryStream();
        using (var deflate = new DeflateStream(buffer, CompressionLevel.Fastest, leaveOpen: true))
        using (var writer = new BinaryWriter(deflate, Encoding.UTF8)) {
            writer.Write(CodecVersion);
            writer.Write7BitEncodedInt(payload.Commands.Count);
            foreach (var command in payload.Commands) {
                WriteCommand(writer, command);
            }
            WriteLayer(writer, payload.Background);
            WriteLayer(writer, payload.Foreground);
        }
        return buffer.ToArray();
    }

    public static FramePayload Decode(byte[] bytes) {
        using var buffer = new MemoryStream(bytes);
        using var deflate = new DeflateStream(buffer, CompressionMode.Decompress);
        using var reader = new BinaryReader(deflate, Encoding.UTF8);

        var version = reader.ReadByte();
        if (version != CodecVersion) {
            throw new InvalidOperationException($"frame codec version {version} is not supported");
        }

        var count = reader.Read7BitEncodedInt();
        var commands = new List<FrameCommand>(count);
        for (var i = 0; i < count; i++) {
            commands.Add(ReadCommand(reader));
        }
        return new FramePayload(commands, ReadLayer(reader), ReadLayer(reader));
    }

    private static void WriteCommand(BinaryWriter writer, FrameCommand command) {
        switch (command) {
            case FrameText text:
                writer.Write(OpText);
                writer.Write(text.Text);
                writer.Write7BitEncodedInt(text.X);
                writer.Write7BitEncodedInt(text.Y);
                writer.Write7BitEncodedInt(text.Size);
                writer.Write(text.Color);
                break;
            case FrameRectangle rectangle:
                writer.Write(OpRectangle);
                writer.Write7BitEncodedInt(rectangle.X);
                writer.Write7BitEncodedInt(rectangle.Y);
                writer.Write7BitEncodedInt(rectangle.Width);
                writer.Write7BitEncodedInt(rectangle.Height);
                writer.Write(rectangle.Color);
                break;
            case FrameBorderRectangle border:
                writer.Write(OpBorderRectangle);
                writer.Write7BitEncodedInt(border.X);
                writer.Write7BitEncodedInt(border.Y);
                writer.Write7BitEncodedInt(border.Width);
                writer.Write7BitEncodedInt(border.Height);
                writer.Write7BitEncodedInt(border.BorderWidth);
                writer.Write(border.Color);
                break;
            case FrameCircle circle:
                writer.Write(OpCircle);
                writer.Write7BitEncodedInt(circle.X);
                writer.Write7BitEncodedInt(circle.Y);
                writer.Write7BitEncodedInt(circle.Radius);
                writer.Write(circle.Color);
                break;
            case FrameBorderCircle borderCircle:
                writer.Write(OpBorderCircle);
                writer.Write7BitEncodedInt(borderCircle.X);
                writer.Write7BitEncodedInt(borderCircle.Y);
                writer.Write7BitEncodedInt(borderCircle.Radius);
                writer.Write7BitEncodedInt(borderCircle.BorderWidth);
                writer.Write(borderCircle.Color);
                break;
            case FrameLine line:
                writer.Write(OpLine);
                writer.Write7BitEncodedInt(line.X1);
                writer.Write7BitEncodedInt(line.Y1);
                writer.Write7BitEncodedInt(line.X2);
                writer.Write7BitEncodedInt(line.Y2);
                writer.Write(line.Color);
                break;
            default:
                throw new InvalidOperationException($"frame command {command.GetType().Name} has no opcode");
        }
    }

    private static FrameCommand ReadCommand(BinaryReader reader) {
        var opcode = reader.ReadByte();
        return opcode switch {
            OpText => new FrameText(reader.ReadString(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.ReadUInt32()),
            OpRectangle => new FrameRectangle(reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.ReadUInt32()),
            OpBorderRectangle => new FrameBorderRectangle(reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.ReadUInt32()),
            OpCircle => new FrameCircle(reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.ReadUInt32()),
            OpBorderCircle => new FrameBorderCircle(reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.ReadUInt32()),
            OpLine => new FrameLine(reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.Read7BitEncodedInt(), reader.ReadUInt32()),
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
