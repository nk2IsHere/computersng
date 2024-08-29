using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace Computers.Game.Utils;

public class GameWindow : IClickableMenu {
    private readonly Action<Rectangle, SpriteBatch> _render;
    private readonly Action<int, int>? _onReceiveLeftClick;
    private readonly Action<int, int>? _onReceiveRightClick;
    private readonly Action<Keys>? _onReceiveKeyPress;
    private readonly Action<int>? _onReceiveScrollWheelAction;
    private readonly Action? _onClose;

    public GameWindow(
        int width,
        int height, 
        Action<Rectangle, SpriteBatch> render,
        Action<int, int>? onReceiveLeftClick = null,
        Action<int, int>? onReceiveRightClick = null,
        Action<Keys>? onReceiveKeyPress = null,
        Action<int>? onReceiveScrollWheelAction = null,
        Action? onClose = null
    ) : base(
        Game1.uiViewport.Width / 2 - (width + borderWidth * 2) / 2,
        Game1.uiViewport.Height / 2 - (height + borderWidth * 2) / 2,
        width + borderWidth * 2,
        height + borderWidth * 2,
        true
    ) {
        _render = render;
        _onReceiveLeftClick = onReceiveLeftClick;
        _onReceiveRightClick = onReceiveRightClick;
        _onReceiveKeyPress = onReceiveKeyPress;
        _onReceiveScrollWheelAction = onReceiveScrollWheelAction;
        _onClose = onClose;
        
        upperRightCloseButton = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + this.width - 36, yPositionOnScreen - 8, 48, 48),
            Game1.mouseCursors, 
            new Rectangle(337, 494, 12, 12), 
            4f
        );
    }

    public override void draw(SpriteBatch batch) {
        batch.Draw(
            Game1.fadeToBlackRect,
            Game1.graphics.GraphicsDevice.Viewport.Bounds, 
            Color.Black * 0.4f
        );

        Game1.drawDialogueBox(
            xPositionOnScreen,
            yPositionOnScreen,
            width,
            height,
            false,
            true
        );

        // Values takes from dialog box code.
        var boundsRectangle = new Rectangle(
            32 + xPositionOnScreen,
            96 + yPositionOnScreen,
            width - 64,
            height - 128
        );

        _render(boundsRectangle, batch);
        base.draw(batch);
        drawMouse(batch);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true) {
        _onReceiveLeftClick?.Invoke(x, y);
        base.receiveLeftClick(x, y, playSound);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true) {
        _onReceiveRightClick?.Invoke(x, y);
        base.receiveRightClick(x, y, playSound);
    }

    public override void receiveKeyPress(Keys key) {
        _onReceiveKeyPress?.Invoke(key);
        
        // HACK: do not close on "E" key press. Too lazy to implement proper handling.
        if (key == Keys.E) {
            return;
        }
        
        base.receiveKeyPress(key);
    }

    public override void receiveScrollWheelAction(int direction) {
        _onReceiveScrollWheelAction?.Invoke(direction);
        base.receiveScrollWheelAction(direction);
    }

    protected override void cleanupBeforeExit() {
        _onClose?.Invoke();
        base.cleanupBeforeExit();
    }
}
