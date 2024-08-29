using Computers.Computer.Boundary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Computers.Computer;

public class RenderComputerState {
    private readonly List<IRenderCommand> _commands = new();
    private readonly Color[] _rawBackground;
    private readonly Color[] _rawForeground;
    
    private readonly Configuration _configuration;
    private readonly Action _onBegin;
    private readonly Action<List<IRenderCommand>, Color[], Color[]> _onEnd;
    
    public RenderComputerState(
        Configuration configuration,
        Action onBegin,
        Action<List<IRenderCommand>, Color[], Color[]> onEnd
    ) {
        _configuration = configuration;
        _onBegin = onBegin;
        _onEnd = onEnd;
        
        _rawBackground = new Color[configuration.CanvasWidth * configuration.CanvasHeight];
        ClearBackground();
        
        _rawForeground = new Color[configuration.CanvasWidth * configuration.CanvasHeight];
        ClearForeground();
    }
    
    public void Begin() {
        ClearCommands();
        ClearForeground();
        ClearBackground();
        _onBegin();
    }

    public void End() {
        lock (_commands) {
            _onEnd(_commands, _rawBackground, _rawForeground);
        }
    }
    
    public void ClearCommands() {
        lock (_commands) {
            _commands.Clear();
        }
    }
    
    public void Text(string text, int x, int y, int size, int[] textColor) {
        var (textColorR, textColorG, textColorB, textColorA) = (textColor[0], textColor[1], textColor[2], textColor[3]);
        lock (_commands) {
            _commands.Add(new TextRenderCommand(
                text,
                x,
                y, 
                size, 
                Game1.dialogueFont, 
                new Color(textColorR, textColorG, textColorB, textColorA)
            ));
        }
    }
    
    public void Rectangle(int x, int y, int width, int height, int[] color) {
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        lock (_commands) {
            _commands.Add(new RectangleRenderCommand(x, y, width, height, new Color(r, g, b, a)));
        }
    }
    
    public void BorderRectangle(int x, int y, int width, int height, int borderWidth, int[] color) {
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        lock (_commands) {
            _commands.Add(new BorderRectangleRenderCommand(x, y, width, height, borderWidth, new Color(r, g, b, a)));
        }
    }
    
    public void Circle(int x, int y, int radius, int[] color) {
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        lock (_commands) {
            _commands.Add(new CircleRenderCommand(x, y, radius, new Color(r, g, b, a)));
        }
    }
    
    public void BorderCircle(int x, int y, int radius, int borderWidth, int[] color) {
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        lock (_commands) {
            _commands.Add(new BorderCircleRenderCommand(x, y, radius, borderWidth, new Color(r, g, b, a)));
        }
    }
    
    public void Line(int x1, int y1, int x2, int y2, int[] color) {
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        lock (_commands) {
            _commands.Add(new LineRenderCommand(x1, y1, x2, y2, new Color(r, g, b, a)));
        }
    }
    
    public void ClearBackground(int[]? color = null) {
        var backgroundColor = color ?? new[] { 0, 0, 0, 255 };
        
        lock(_rawBackground) {
            for (var i = 0; i < _rawBackground.Length; i++) {
                _rawBackground[i] = new Color(backgroundColor[0], backgroundColor[1], backgroundColor[2], backgroundColor[3]);
            }
        }
    }
    
    public void ClearForeground() {
        lock(_rawForeground) {
            for (var i = 0; i < _rawForeground.Length; i++) {
                _rawForeground[i] = Color.Transparent;
            }
        }
    }
    
    public void SetBackground(int x, int y, int[] color) {
        if (x < 0 || x >= _configuration.CanvasWidth || y < 0 || y >= _configuration.CanvasHeight) {
            return;
        }
        
        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        lock (_rawBackground) {
            _rawBackground[y * _configuration.CanvasWidth + x] = new Color(r, g, b, a);
        }
    }

    public void SetForeground(int x, int y, int[] color) {
        if (x < 0 || x >= _configuration.CanvasWidth || y < 0 || y >= _configuration.CanvasHeight) {
            return;
        }

        var (r, g, b, a) = (color[0], color[1], color[2], color[3]);
        lock (_rawForeground) {
            _rawForeground[y * _configuration.CanvasWidth + x] = new Color(r, g, b, a);
        }
    }
}

public class RenderComputerApi: IComputerApi {
    public string Name => "Render";
    public bool ShouldExpose => true;
    public object Api => _state;
    public List<Type> ReceivableEvents => new() { typeof(RenderComputerEvent) };
    
    private readonly Configuration _configuration;
    
    private readonly RenderComputerState _state;

    private readonly List<IRenderCommand> _renderCommandsCopy;
    private readonly Color[] _renderBackgroundCopy;
    private readonly Color[] _renderForegroundCopy;
    
    private readonly Color[] _renderData;
    private readonly Texture2D _renderTexture;
    
    public RenderComputerApi(
        Configuration configuration
    ) {
        _configuration = configuration;
        _renderData = new Color[configuration.CanvasWidth * configuration.CanvasHeight];
        
        _renderCommandsCopy = new List<IRenderCommand>();
        _renderBackgroundCopy = new Color[configuration.CanvasWidth * configuration.CanvasHeight];
        _renderForegroundCopy = new Color[configuration.CanvasWidth * configuration.CanvasHeight];

        _renderTexture = new Texture2D(Game1.graphics.GraphicsDevice, configuration.CanvasWidth, configuration.CanvasHeight, false, SurfaceFormat.Color);
        
        _state = new RenderComputerState(
            configuration,
            () => { },
            (commands, background, foreground) => {
                // Copy the render data to avoid concurrent modification from main thread of computer
                
                lock (_renderCommandsCopy) {
                    _renderCommandsCopy.Clear();
                    _renderCommandsCopy.AddRange(commands);
                }

                lock (_renderBackgroundCopy) {
                    for (var i = 0; i < background.Length; i++) {
                        _renderBackgroundCopy[i] = background[i];
                    }
                }

                lock (_renderForegroundCopy) {
                    for (var i = 0; i < foreground.Length; i++) {
                        _renderForegroundCopy[i] = foreground[i];
                    }
                }
            }
        );
    }
    
    public void ReceiveEvent(IComputerEvent computerEvent) {
        var (rectangle, batch) = computerEvent.Data<(Rectangle, SpriteBatch)>();
        // Fill the render data with the raw background
        lock (_renderBackgroundCopy) {
            for (var i = 0; i < _renderBackgroundCopy.Length; i++) {
                _renderData[i] = _renderBackgroundCopy[i];
            }
        }

        lock (_renderCommandsCopy) {
            foreach (var command in _renderCommandsCopy) {
                command.Draw(_renderData, _configuration.CanvasWidth, _configuration.CanvasHeight);
            }
        }
        
        // Fill the raw foreground with the render data by blending the render data with the raw foreground
        lock (_renderForegroundCopy) {
            for (var i = 0; i < _renderForegroundCopy.Length; i++) {
                _renderForegroundCopy[i] = Color.Lerp(_renderForegroundCopy[i], _renderData[i], _renderData[i].A / 255f);
            }
        }
        
        _renderTexture.SetData(
            0,
            new Rectangle(0, 0, _configuration.CanvasWidth, _configuration.CanvasHeight),
            _renderData,
            0,
            _configuration.CanvasWidth * _configuration.CanvasHeight
        );
        
        batch.Draw(_renderTexture, rectangle, Color.White);
    }

    public void Reset() {
        _state.ClearCommands();
        _state.ClearBackground();
        _state.ClearForeground();
    }
}
