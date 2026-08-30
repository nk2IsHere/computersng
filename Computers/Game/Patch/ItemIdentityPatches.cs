using Computers.Core;
using HarmonyLib;
using StardewValley;
using Object = StardewValley.Object;

namespace Computers.Game.Patch;

public static class ItemIdentityPatches {
    private static readonly string[] IdentityKeys = { "ComputerId", "RouterId", "PeripheralId" };

    public static void Apply(string harmonyId) {
        var harmony = new Harmony(harmonyId);

        harmony.Patch(
            original: AccessTools.Method(typeof(Item), nameof(Item.canStackWith)),
            postfix: new HarmonyMethod(typeof(ItemIdentityPatches), nameof(CanStackWithPostfix))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Object), nameof(Object.getDescription)),
            postfix: new HarmonyMethod(typeof(ItemIdentityPatches), nameof(GetDescriptionPostfix))
        );
    }

    internal static void CanStackWithPostfix(Item __instance, ISalable other, ref bool __result) {
        if (!__result || other is not Item otherItem) {
            return;
        }

        foreach (var key in IdentityKeys) {
            __instance.modData.TryGetValue(key, out var thisId);
            otherItem.modData.TryGetValue(key, out var otherId);

            if (thisId == otherId) continue;
            __result = false;
            return;
        }
    }

    internal static void GetDescriptionPostfix(Object __instance, ref string __result) {
        foreach (var key in IdentityKeys) {
            if (!__instance.modData.TryGetValue(key, out var identity)) continue;
            __result += $"\nID: {identity.AsId().Last}";
            return;
        }
    }
}
