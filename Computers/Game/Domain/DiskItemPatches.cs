using Computers.Core;
using HarmonyLib;
using StardewValley;
using Object = StardewValley.Object;

namespace Computers.Game.Domain;

public static class DiskItemPatches {
    private const string ComputerIdKey = "ComputerId";

    public static void Apply(string harmonyId) {
        var harmony = new Harmony(harmonyId);

        harmony.Patch(
            original: AccessTools.Method(typeof(Item), nameof(Item.canStackWith)),
            postfix: new HarmonyMethod(typeof(DiskItemPatches), nameof(CanStackWithPostfix))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Object), nameof(Object.getDescription)),
            postfix: new HarmonyMethod(typeof(DiskItemPatches), nameof(GetDescriptionPostfix))
        );
    }

    internal static void CanStackWithPostfix(Item __instance, ISalable other, ref bool __result) {
        if (!__result || other is not Item otherItem) {
            return;
        }

        __instance.modData.TryGetValue(ComputerIdKey, out var thisId);
        otherItem.modData.TryGetValue(ComputerIdKey, out var otherId);

        if (thisId != otherId) {
            __result = false;
        }
    }

    internal static void GetDescriptionPostfix(Object __instance, ref string __result) {
        if (__instance.modData.TryGetValue(ComputerIdKey, out var computerId)) {
            __result += $"\nID: {computerId.AsId().Last}";
        }
    }
}
