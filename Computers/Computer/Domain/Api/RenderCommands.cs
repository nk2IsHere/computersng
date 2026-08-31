using Computers.Computer.Utils;
using Microsoft.Xna.Framework;

namespace Computers.Computer.Domain.Api;

public interface IRenderCommand {
    void Draw(
        Color[] data,
        int canvasWidth,
        int canvasHeight
    );

    void Encode(BinaryWriter writer);
}

public record TextRenderCommand(
    string Text,
    int X,
    int Y,
    int Size,
    BmFont Font,
    Color Color
) : IRenderCommand {
    internal const byte Opcode = 1;

    public void Draw(
        Color[] data,
        int canvasWidth,
        int canvasHeight
    ) {
        var fontSize = (float) Font.GlyphSize();
        var scaledSize = Math.Min(Font.MaxScale(), Size / fontSize);

        Font.Draw(data, canvasWidth, canvasHeight, X, Y, Text, scaledSize, Color);
    }

    public void Encode(BinaryWriter writer) {
        writer.Write(Opcode);
        writer.Write(Text);
        writer.Write7BitEncodedInt(X);
        writer.Write7BitEncodedInt(Y);
        writer.Write7BitEncodedInt(Size);
        writer.Write(Color.Pack());
    }

    internal static TextRenderCommand Decode(BinaryReader reader, BmFont font) {
        return new TextRenderCommand(
            reader.ReadString(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            font,
            reader.ReadUInt32().Unpack()
        );
    }
}

public record RectangleRenderCommand(
    int X,
    int Y,
    int Width,
    int Height,
    Color Color
) : IRenderCommand {
    internal const byte Opcode = 2;

    public void Draw(
        Color[] data,
        int canvasWidth,
        int canvasHeight
    ) {
        for (var i = 0; i < data.Length; i++) {
            var row = i / canvasWidth;
            var col = i % canvasWidth;
            if (col >= X && col < X + Width && row >= Y && row < Y + Height) {
                data[i] = Color;
            }
        }
    }

    public void Encode(BinaryWriter writer) {
        writer.Write(Opcode);
        writer.Write7BitEncodedInt(X);
        writer.Write7BitEncodedInt(Y);
        writer.Write7BitEncodedInt(Width);
        writer.Write7BitEncodedInt(Height);
        writer.Write(Color.Pack());
    }

    internal static RectangleRenderCommand Decode(BinaryReader reader) {
        return new RectangleRenderCommand(
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.ReadUInt32().Unpack()
        );
    }
}

public record BorderRectangleRenderCommand(
    int X,
    int Y,
    int Width,
    int Height,
    int BorderWidth,
    Color Color
) : IRenderCommand {
    internal const byte Opcode = 3;

    public void Draw(
        Color[] data,
        int canvasWidth,
        int canvasHeight
    ) {
        for (var i = 0; i < data.Length; i++) {
            var row = i / canvasWidth;
            var col = i % canvasWidth;
            if (col < X || col >= X + Width || row < Y || row >= Y + Height) {
                continue;
            }

            if (col < X + BorderWidth || col >= X + Width - BorderWidth || row < Y + BorderWidth || row >= Y + Height - BorderWidth) {
                data[i] = Color;
            }
        }
    }

    public void Encode(BinaryWriter writer) {
        writer.Write(Opcode);
        writer.Write7BitEncodedInt(X);
        writer.Write7BitEncodedInt(Y);
        writer.Write7BitEncodedInt(Width);
        writer.Write7BitEncodedInt(Height);
        writer.Write7BitEncodedInt(BorderWidth);
        writer.Write(Color.Pack());
    }

    internal static BorderRectangleRenderCommand Decode(BinaryReader reader) {
        return new BorderRectangleRenderCommand(
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.ReadUInt32().Unpack()
        );
    }
}

public record CircleRenderCommand(
    int X,
    int Y,
    int Radius,
    Color Color
) : IRenderCommand {
    internal const byte Opcode = 4;

    public void Draw(
        Color[] data,
        int canvasWidth,
        int canvasHeight
    ) {
        for (var i = 0; i < data.Length; i++) {
            var row = i / canvasWidth;
            var col = i % canvasWidth;
            var distance = new Vector2(col - X, row - Y);
            if (distance.Length() <= Radius) {
                data[i] = Color;
            }
        }
    }

    public void Encode(BinaryWriter writer) {
        writer.Write(Opcode);
        writer.Write7BitEncodedInt(X);
        writer.Write7BitEncodedInt(Y);
        writer.Write7BitEncodedInt(Radius);
        writer.Write(Color.Pack());
    }

    internal static CircleRenderCommand Decode(BinaryReader reader) {
        return new CircleRenderCommand(
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.ReadUInt32().Unpack()
        );
    }
}

public record BorderCircleRenderCommand(
    int X,
    int Y,
    int Radius,
    int BorderWidth,
    Color Color
) : IRenderCommand {
    internal const byte Opcode = 5;

    public void Draw(
        Color[] data,
        int canvasWidth,
        int canvasHeight
    ) {
        for (var i = 0; i < data.Length; i++) {
            var row = i / canvasWidth;
            var col = i % canvasWidth;
            var distance = new Vector2(col - X, row - Y);
            if (distance.Length() <= Radius && distance.Length() > Radius - BorderWidth) {
                data[i] = Color;
            }
        }
    }

    public void Encode(BinaryWriter writer) {
        writer.Write(Opcode);
        writer.Write7BitEncodedInt(X);
        writer.Write7BitEncodedInt(Y);
        writer.Write7BitEncodedInt(Radius);
        writer.Write7BitEncodedInt(BorderWidth);
        writer.Write(Color.Pack());
    }

    internal static BorderCircleRenderCommand Decode(BinaryReader reader) {
        return new BorderCircleRenderCommand(
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.ReadUInt32().Unpack()
        );
    }
}

public record LineRenderCommand(
    int X1,
    int Y1,
    int X2,
    int Y2,
    Color Color
) : IRenderCommand {
    internal const byte Opcode = 6;

    public void Draw(
        Color[] data,
        int canvasWidth,
        int canvasHeight
    ) {
        var x1 = X1;
        var y1 = Y1;
        var x2 = X2;
        var y2 = Y2;
        var dx = Math.Abs(x2 - x1);
        var dy = Math.Abs(y2 - y1);
        var sx = x1 < x2 ? 1 : -1;
        var sy = y1 < y2 ? 1 : -1;
        var err = dx - dy;

        while (true) {
            var index = y1 * canvasWidth + x1;
            if (index >= 0 && index < data.Length) {
                data[index] = Color;
            }

            if (x1 == x2 && y1 == y2) {
                break;
            }

            var e2 = 2 * err;
            if (e2 > -dy) {
                err -= dy;
                x1 += sx;
            }

            if (e2 < dx) {
                err += dx;
                y1 += sy;
            }
        }
    }

    public void Encode(BinaryWriter writer) {
        writer.Write(Opcode);
        writer.Write7BitEncodedInt(X1);
        writer.Write7BitEncodedInt(Y1);
        writer.Write7BitEncodedInt(X2);
        writer.Write7BitEncodedInt(Y2);
        writer.Write(Color.Pack());
    }

    internal static LineRenderCommand Decode(BinaryReader reader) {
        return new LineRenderCommand(
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt(),
            reader.ReadUInt32().Unpack()
        );
    }
}
