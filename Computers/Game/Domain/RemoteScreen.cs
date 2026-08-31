using Computers.Computer;
using Computers.Computer.Domain.Api;
using Computers.Computer.Utils;
using Computers.Game.Utils;
using Computers.Multiplayer.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewValley;
using GameWindow = Computers.Game.Utils.GameWindow;

namespace Computers.Game.Domain;

public static class RemoteScreen {
    public static void Open(
        ClientChannel channel,
        string computerId,
        Configuration configuration,
        IRedundantLoader assetLoader
    ) {
        var width = configuration.Render.CanvasWidth;
        var height = configuration.Render.CanvasHeight;
        var font = BmFont.Load(
            assetLoader.Load<string>(configuration.Resource.FontDefinitionPath),
            assetLoader.Load<Texture2D>(configuration.Resource.FontTexturePath)
        );

        var background = new Color[width * height];
        Array.Fill(background, new Color(0, 0, 0, 255));
        var foreground = new Color[width * height];
        Array.Fill(foreground, Color.Transparent);
        var canvas = new Color[width * height];
        var texture = new Texture2D(Game1.graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color);
        var dirty = true;
        GameWindow? window = null;

        void OnHostEvent(JObject payload) {
            if (payload["event"]?.Value<string>() == "screenClosed"
                && payload["computerId"]?.Value<string>() == computerId
                && ReferenceEquals(Game1.activeClickableMenu, window)) {
                Game1.exitActiveMenu();
            }
        }

        channel.FrameReceived += ApplyFrame;
        channel.EventReceived += OnHostEvent;

        void Input(string kind, int a, int b) {
            channel.Send("screenInput", new { computerId, kind, a, b });
        }

        window = new GameWindow(
            configuration.Ui.WindowWidth,
            configuration.Ui.WindowHeight,
            (rectangle, batch) => {
                if (dirty) {
                    texture.SetData(0, new Rectangle(0, 0, width, height), canvas, 0, canvas.Length);
                    dirty = false;
                }
                batch.Draw(texture, rectangle, new Rectangle(0, 0, width, height), Color.White);
            },
            onReceiveLeftClick: (x, y) => Input("leftClick", x, y),
            onReceiveRightClick: (x, y) => Input("rightClick", x, y),
            onReceiveKeyPress: key => Input("key", (int) key, 0),
            onReceiveScrollWheelAction: direction => Input("wheel", direction, 0),
            onClose: () => {
                channel.FrameReceived -= ApplyFrame;
                channel.EventReceived -= OnHostEvent;
                channel.Send("closeScreen", new { computerId });
            }
        );

        Game1.InUIMode(() => {
            Game1.activeClickableMenu = window!;
        });

        void ApplyFrame(string frameComputerId, bool snapshot, byte[] body) {
            if (frameComputerId != computerId) {
                return;
            }
            FramePayload payload;
            try {
                payload = FrameCodec.Decode(body);
            } catch (Exception) {
                return;
            }
            if (payload.Background is not null) {
                FramePayloadConverter.DecodeLayer(payload.Background, background);
            }
            if (payload.Foreground is not null) {
                FramePayloadConverter.DecodeLayer(payload.Foreground, foreground);
            }
            FrameComposer.Compose(
                canvas,
                background,
                FramePayloadConverter.ToRenderCommands(payload.Commands, font!),
                foreground,
                width,
                height
            );
            dirty = true;
        }
    }
}
