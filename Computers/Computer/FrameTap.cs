using Computers.Computer.Domain.Api;
using Microsoft.Xna.Framework;

namespace Computers.Computer;

// A tap on the computer's composed frames for host side consumers, expressed purely
// in the computer's own terms. WantsFrames lets the render pipeline skip the call
// entirely when nobody consumes, and OnFrame runs on the scheduler worker inside the
// frame slice, so implementations must copy what they keep.
public interface IFrameTap {
    bool WantsFrames(string computerId);

    void OnFrame(
        string computerId,
        IReadOnlyList<IRenderCommand> commands,
        Color[] background,
        Color[] foreground,
        int rawVersion
    );
}
