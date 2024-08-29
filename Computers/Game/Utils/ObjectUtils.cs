using StardewValley.Mods;

namespace Computers.Game.Utils;
using Object = StardewValley.Object;

public static class ObjectUtils {

    public static ModDataDictionary? HeldObjectModData(
        this Object svObject
    ) {
        var heldObject = svObject.heldObject.Value;
        return heldObject?.modData;
    }
}
