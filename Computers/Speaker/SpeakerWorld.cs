using Computers.Router.Domain;

namespace Computers.Speaker;

public interface ISpeakerWorld {
    bool Play(Placement placement, string cue, int? pitch);
}
