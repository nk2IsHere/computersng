using Microsoft.Xna.Framework;

namespace Computers.Computer.Domain.Api;

public static class FrameComposer {
    public static void Compose(
        Color[] target,
        Color[] background,
        List<IRenderCommand> commands,
        Color[] foreground,
        int width,
        int height
    ) {
        Array.Copy(background, target, target.Length);

        foreach (var command in commands) {
            command.Draw(target, width, height);
        }

        for (var i = 0; i < target.Length; i++) {
            var overlay = foreground[i];
            if (overlay.A == 0) {
                continue;
            }

            target[i] = overlay.A == byte.MaxValue
                ? overlay
                : Color.Lerp(target[i], overlay, overlay.A / 255f);
        }
    }
}
