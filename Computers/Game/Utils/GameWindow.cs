using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Computers.Game.Utils;

public class GameWindow : IClickableMenu {
    private readonly int _width;
    private readonly int _height;
    private readonly Action<SpriteBatch> _render;

    public GameWindow(int width, int height, Action<SpriteBatch> render) : base(
        Game1.uiViewport.Width / 2 - (width + borderWidth * 2) / 2,
        Game1.uiViewport.Height / 2 - (height + borderWidth * 2) / 2,
        width + borderWidth * 2,
        height + borderWidth * 2,
        true
    ) {
        _width = width;
        _height = height;
        _render = render;
        upperRightCloseButton = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + this.width - 36, yPositionOnScreen - 8, 48, 48),
            Game1.mouseCursors, 
            new Rectangle(337, 494, 12, 12), 4f
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

        _render(batch);

        base.draw(batch);
        drawMouse(batch);
    }
}