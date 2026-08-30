using Computers.Router.Domain;
using Computers.Speaker;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Computers.Game.Domain;

// The Stardew implementation of the speaker's world port.
public class StardewSpeakerWorld : ISpeakerWorld {
    public bool Play(Placement placement, string cue, int? pitch) {
        var location = Game1.getLocationFromName(placement.Location);
        if (location is null) {
            return false;
        }

        try {
            location.playSound(cue, new Vector2(placement.X, placement.Y), pitch);
            return true;
        } catch (Exception) {
            return false;
        }
    }
}
