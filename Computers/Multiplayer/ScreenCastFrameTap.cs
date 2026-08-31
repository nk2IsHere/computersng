using System.Collections.Concurrent;
using Computers.Computer;
using Computers.Computer.Domain.Api;
using Computers.Multiplayer.Domain;
using Microsoft.Xna.Framework;

namespace Computers.Multiplayer;

// Bridges the computer's frame tap to the screen cast. All multiplayer knowledge,
// the payload conversion and the raw layer freshness tracking, lives on this side of
// the seam so the computer domain never references it.
public sealed class ScreenCastFrameTap : IFrameTap {
    private readonly IScreenCastSink _sink;
    private readonly ConcurrentDictionary<string, int> _lastRawVersions = new();

    public ScreenCastFrameTap(IScreenCastSink sink) {
        _sink = sink;
    }

    public bool WantsFrames(string computerId) {
        return _sink.HasViewers(computerId);
    }

    public void OnFrame(
        string computerId,
        IReadOnlyList<IRenderCommand> commands,
        Color[] background,
        Color[] foreground,
        int rawVersion
    ) {
        var includeRaw = rawVersion != _lastRawVersions.GetValueOrDefault(computerId, 0);
        _sink.PublishFrame(
            computerId,
            FramePayloadConverter.Convert(commands, background, foreground, includeRaw),
            rawVersion
        );
        _lastRawVersions[computerId] = rawVersion;
    }
}
