using Computers.Computer.Boundary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Computers.Computer;

public class RenderComputerApi: IComputerApi {
    public string Name => "Render";
    public bool ShouldExpose => true;
    public object Api => this;
    public List<Type> ReceivableEvents => new() { typeof(RenderComputerEvent) };
    
    private readonly Configuration _configuration;

    private readonly List<IRenderCommand> _commands = new();
    private readonly Color[] _renderData;
    private readonly Texture2D _renderTexture;
    
    public RenderComputerApi(
        Configuration configuration
    ) {
        _configuration = configuration;
        _renderData = new Color[configuration.WindowWidth * configuration.WindowHeight];
        _renderTexture = new Texture2D(Game1.graphics.GraphicsDevice, configuration.WindowWidth, configuration.WindowHeight, false, SurfaceFormat.Color);
    }
    
    public void ReceiveEvent(IComputerEvent computerEvent) {
        var (rectangle, batch) = computerEvent.Data<(Rectangle, SpriteBatch)>();
        // Clear the render data
        for (var i = 0; i < _renderData.Length; i++) {
            _renderData[i] = Color.Black;
        }
        
        foreach (var command in _commands) {
            command.Draw(_renderData, _configuration.WindowWidth, _configuration.WindowHeight);
        }
        
        _renderTexture.SetData(
            0,
            new Rectangle(0, 0, _configuration.WindowWidth, _configuration.WindowHeight),
            _renderData,
            0,
            _configuration.WindowWidth * _configuration.WindowHeight
        );
        
        batch.Draw(_renderTexture, rectangle, Color.White);
    }

    public void Reset() {
        _commands.Clear();
    }

    public void Clear() {
        Reset();
    }
    
    public void Text(string text, int x, int y, int size, int r, int g, int b) {
        var color = new Color(r, g, b);
        _commands.Add(new TextRenderCommand(text, x, y, size, Game1.smallFont, color));
    }
    
    public void Rectangle(int x, int y, int width, int height, int r, int g, int b) {
        var color = new Color(r, g, b);
        _commands.Add(new RectangleRenderCommand(x, y, width, height, color));
    }
    
    public void BorderRectangle(int x, int y, int width, int height, int borderWidth, int r, int g, int b) {
        var color = new Color(r, g, b);
        _commands.Add(new BorderRectangleRenderCommand(x, y, width, height, borderWidth, color));
    }
    
    public void Circle(int x, int y, int radius, int r, int g, int b) {
        var color = new Color(r, g, b);
        _commands.Add(new CircleRenderCommand(x, y, radius, color));
    }
    
    public void BorderCircle(int x, int y, int radius, int borderWidth, int r, int g, int b) {
        var color = new Color(r, g, b);
        _commands.Add(new BorderCircleRenderCommand(x, y, radius, borderWidth, color));
    }
    
    public void Line(int x1, int y1, int x2, int y2, int r, int g, int b) {
        var color = new Color(r, g, b);
        _commands.Add(new LineRenderCommand(x1, y1, x2, y2, color));
    }
}
