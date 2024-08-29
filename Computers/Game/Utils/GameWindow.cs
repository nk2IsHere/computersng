using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Computers.Game.Utils;

public class GameWindow : IClickableMenu {
    private readonly Action<Rectangle, SpriteBatch> _render;

    public GameWindow(int width, int height, Action<Rectangle, SpriteBatch> render) : base(
        Game1.uiViewport.Width / 2 - (width + borderWidth * 2) / 2,
        Game1.uiViewport.Height / 2 - (height + borderWidth * 2) / 2,
        width + borderWidth * 2 + 64,
        height + borderWidth * 2 + 128,
        true
    ) {
        _render = render;
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
}