using Computers.Computer.Utils;
using Computers.Game;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using TripleFrameBuffer = Computers.Computer.Utils.TripleBuffer<Microsoft.Xna.Framework.Color[]>;

namespace Computers.Computer.Domain.Api;

public class RenderComputerApi: IComputerApi {
    public string Name => "Render";
    public bool ShouldExpose => true;
    public object Api => _state;
    
    public ISet<Type> ReceivableEvents => new HashSet<Type> { typeof(RenderComputerEvent) };
    public IRedundantLoader? LibraryLoader => null;

    private readonly Configuration _configuration;

    private readonly RenderComputerState _state;

    private readonly TripleFrameBuffer _frames;
    private readonly Texture2D _renderTexture;
    
    public RenderComputerApi(
        IComputerPort computerPort,
        IFrameTap frameTap
    ) {
        _configuration = computerPort.Configuration;
        var pixelCount = _configuration.Render.CanvasWidth * _configuration.Render.CanvasHeight;
        _frames = new TripleFrameBuffer(new Color[pixelCount], new Color[pixelCount], new Color[pixelCount]);

        _renderTexture = new Texture2D(
            Game1.graphics.GraphicsDevice,
            _configuration.Render.CanvasWidth,
            _configuration.Render.CanvasHeight,
            false,
            SurfaceFormat.Color
        );

        var font = BmFont.Load(
            computerPort.LoadAsset<string>(_configuration.Resource.FontDefinitionPath),
            computerPort.LoadAsset<Texture2D>(_configuration.Resource.FontTexturePath)
        );

        _state = new RenderComputerState(
            _configuration,
            font!,
            () => { },
            (commands, background, foreground) => {
                FrameComposer.Compose(
                    _frames.ProduceSlot,
                    background,
                    commands,
                    foreground,
                    _configuration.Render.CanvasWidth,
                    _configuration.Render.CanvasHeight
                );
                _frames.Publish();

                if (!frameTap.WantsFrames(computerPort.Id)) {
                    return;
                }

                frameTap.OnFrame(computerPort.Id, commands, background, foreground, _state.RawVersion);
            }
        );
    }

    public void ReceiveEvent(IComputerEvent computerEvent) {
        var (destinationRectangle, batch) = computerEvent.Data<(Rectangle, SpriteBatch)>();
        var sourceRectangle = new Rectangle(0, 0, _configuration.Render.CanvasWidth, _configuration.Render.CanvasHeight);

        var (frame, isNew) = _frames.Consume();
        if (isNew) {
            _renderTexture.SetData(
                0,
                sourceRectangle,
                frame,
                0,
                frame.Length
            );
        }

        batch.Draw(_renderTexture, destinationRectangle, sourceRectangle, Color.White);
    }

    public void Reset() {
        _state.ClearCommands();
        _state.ClearBackground();
        _state.ClearForeground();
    }
}

internal class RenderComputerState {
    private readonly List<IRenderCommand> _commands = new();
    private readonly Color[] _rawBackground;
    private readonly Color[] _rawForeground;

    private readonly Configuration _configuration;
    private readonly BmFont _font;

    private readonly Action _onBegin;
    private readonly Action<List<IRenderCommand>, Color[], Color[]> _onEnd;

    public int RawVersion { get; private set; }

    public RenderComputerState(
        Configuration configuration,
        BmFont font,
        Action onBegin,
        Action<List<IRenderCommand>, Color[], Color[]> onEnd
    ) {
        _configuration = configuration;
        _font = font;
        _onBegin = onBegin;
        _onEnd = onEnd;

        _rawBackground = new Color[configuration.Render.CanvasWidth * configuration.Render.CanvasHeight];
        ClearBackground();

        _rawForeground = new Color[configuration.Render.CanvasWidth * configuration.Render.CanvasHeight];
        ClearForeground();
    }

    public void Begin() {
        ClearCommands();
        _onBegin();
    }

    public void End() {
        _onEnd(_commands, _rawBackground, _rawForeground);
    }
    
    public void Text(int x, int y, string text, int size, int[] textColor) {
        if (x < 0 || y < 0) {
            return;
        }
        
        if (size <= 0) {
            size = GetDefaultFontSize();
        }
        
        if (size > GetMaximalFontSize()) {
            size = GetMaximalFontSize();
        }
        
        if (textColor is not { Length: 4 }) {
            return;
        }
        
        if (string.IsNullOrEmpty(text)) {
            return;
        }
        
        var (textColorR, textColorG, textColorB, textColorA) = (textColor[0], textColor[1], textColor[2], textColor[3]);
        _commands.Add(new TextRenderCommand(
            text,
            x,
            y, 
            size,
            _font,
            new Color(textColorR, textColorG, textColorB, textColorA)
        ));
    }
    
    public void Rectangle(int x, int y, int width, int height, int[]? color) {
        if (x < 0 || y < 0) {
            return;
        }
        
        if (width <= 0 || height <= 0) {
            return;
        }
        
        if (color is not { Length: 4 }) {
            return;
        }
        
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        _commands.Add(new RectangleRenderCommand(x, y, width, height, new Color(r, g, b, a)));

    }
    
    public void BorderRectangle(int x, int y, int width, int height, int borderWidth, int[] color) {
        if (x < 0 || y < 0) {
            return;
        }
        
        if (width <= 0 || height <= 0) {
            return;
        }
        
        if (borderWidth <= 0) {
            return;
        }
        
        if (color is not { Length: 4 }) {
            return;
        }
        
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        _commands.Add(new BorderRectangleRenderCommand(x, y, width, height, borderWidth, new Color(r, g, b, a)));
    }
    
    public void Circle(int x, int y, int radius, int[] color) {
        if (x < 0 || y < 0) {
            return;
        }
        
        if (radius <= 0) {
            return;
        }
        
        if (color is not { Length: 4 }) {
            return;
        }
        
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        _commands.Add(new CircleRenderCommand(x, y, radius, new Color(r, g, b, a)));

    }
    
    public void BorderCircle(int x, int y, int radius, int borderWidth, int[] color) {
        if (x < 0 || y < 0) {
            return;
        }
        
        if (radius <= 0) {
            return;
        }
        
        if (borderWidth <= 0) {
            return;
        }
        
        if (color is not { Length: 4 }) {
            return;
        }
        
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        _commands.Add(new BorderCircleRenderCommand(x, y, radius, borderWidth, new Color(r, g, b, a)));
    }
    
    public void Line(int x1, int y1, int x2, int y2, int[] color) {
        if (x1 < 0 || y1 < 0) {
            return;
        }
        
        if (x2 < 0 || y2 < 0) {
            return;
        }
        
        if (color is not { Length: 4 }) {
            return;
        }
        
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        _commands.Add(new LineRenderCommand(x1, y1, x2, y2, new Color(r, g, b, a)));
    }
    
    public void ClearCommands() {
        _commands.Clear();
    }
    
    private static readonly int[] DefaultBackgroundColor = { 0, 0, 0, 255 };

    private void MarkRawChanged() {
        RawVersion++;
    }

    public void ClearBackground(int[]? color = null) {
        MarkRawChanged();
        var backgroundColor = color ?? DefaultBackgroundColor;
        var fill = new Color(backgroundColor[0], backgroundColor[1], backgroundColor[2], backgroundColor[3]);
        for (var i = 0; i < _rawBackground.Length; i++) {
            _rawBackground[i] = fill;
        }
    }
    
    public void ClearForeground() {
        MarkRawChanged();
        for (var i = 0; i < _rawForeground.Length; i++) {
            _rawForeground[i] = Color.Transparent;
        }
    }
    
    public void SetBackground(int x, int y, int[] color) {
        MarkRawChanged();
        if (x < 0 || x >= _configuration.Render.CanvasWidth || y < 0 || y >= _configuration.Render.CanvasHeight) {
            return;
        }
        
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        _rawBackground[y * _configuration.Render.CanvasWidth + x] = new Color(r, g, b, a);
    }

    public void SetForeground(int x, int y, int[] color) {
        MarkRawChanged();
        if (x < 0 || x >= _configuration.Render.CanvasWidth || y < 0 || y >= _configuration.Render.CanvasHeight) {
            return;
        }

        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        _rawForeground[y * _configuration.Render.CanvasWidth + x] = new Color(r, g, b, a);
    }
    
    public int[] GetScreenBoundaries() {
        return new[] { _configuration.Render.CanvasWidth, _configuration.Render.CanvasHeight };
    }

    public int GetMaximalFontSize() {
        return (int) (_font.GlyphSize() * _font.MaxScale());
    }
    
    public int GetDefaultFontSize() {
        return (int) (_font.GlyphSize() * _configuration.Render.FontDefaultScale);
    }
    
    public int[] MeasureGlyphSize(char c, int size) {
        if (size <= 0) {
            size = GetDefaultFontSize();
        }
        
        if (size > GetMaximalFontSize()) {
            size = GetMaximalFontSize();
        }

        var scale = size * 1.0f / _font.GlyphSize();
        var (width, height) = _font.MeasureGlyph(c, scale);
        
        return new[] { width, height };
    }
    
    public int[] MeasureTextWidth(string text, int size) {
        if (size <= 0) {
            size = GetDefaultFontSize();
        }
        
        if (size > GetMaximalFontSize()) {
            size = GetMaximalFontSize();
        }
        
        if (string.IsNullOrEmpty(text)) {
            return new[] { 0, 0 };
        }
        
        var scale = size * 1.0f / _font.GlyphSize();
        var (textWidth, textHeight) = _font.Measure(text, scale);
        
        return new[] { textWidth, textHeight };
    }
}
