using Computers.Computer.Utils;
using Microsoft.Xna.Framework;

namespace Computers.Computer.Domain.Api;

public interface IRenderCommand {
    void Draw(
        Color[] data,
        int canvasWidth,
        int canvasHeight
    );
}

public record TextRenderCommand(
    string Text,
    int X,
    int Y,
    int Size,
    BmFont Font,
    Color Color
) : IRenderCommand {

    public void Draw(
        Color[] data,
        int canvasWidth,
        int canvasHeight
    ) {
        var fontSize = (float) Font.GlyphSize();
        var scaledSize = Math.Min(Font.MaxScale(), Size / fontSize);
        
        Font.Draw(data, canvasWidth, canvasHeight, X, Y, Text, scaledSize, Color);
    }
}

public record RectangleRenderCommand(
    int X,
    int Y,
    int Width,
    int Height,
    Color Color
) : IRenderCommand {

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
}

public record BorderRectangleRenderCommand(
    int X,
    int Y,
    int Width,
    int Height,
    int BorderWidth,
    Color Color
) : IRenderCommand {
    
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

}

public record CircleRenderCommand(
    int X,
    int Y,
    int Radius,
    Color Color
) : IRenderCommand {
    
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
}

public record BorderCircleRenderCommand(
    int X,
    int Y,
    int Radius,
    int BorderWidth,
    Color Color
) : IRenderCommand {
    
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
}

public record LineRenderCommand(
    int X1,
    int Y1,
    int X2,
    int Y2,
    Color Color
) : IRenderCommand {
    
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
}
