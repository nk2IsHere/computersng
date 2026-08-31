using Computers.Multiplayer.Domain;

namespace Computers.Multiplayer;

public interface IScreenCastSink {
    bool HasViewers(string computerId);

    void PublishFrame(string computerId, FramePayload payload, int rawVersion);
}

public sealed class NullScreenCastSink : IScreenCastSink {
    public bool HasViewers(string computerId) => false;

    public void PublishFrame(string computerId, FramePayload payload, int rawVersion) {
    }
}
