using Computers.Multiplayer.Domain;

namespace Computers.Multiplayer;

public interface IScreenCastSink {
    bool HasViewers(string computerId);

    void PublishFrame(string computerId, FramePayload payload, int rawVersion);
}
