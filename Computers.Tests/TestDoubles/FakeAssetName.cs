using StardewModdingAPI;

namespace Computers.Tests.TestDoubles;

/// <summary>
/// IAssetName has an internal member, so it cannot be implemented outside SMAPI.
/// Instead, instantiate SMAPI's own internal AssetName via its public constructor.
/// </summary>
public static class FakeAssetName {
    private static readonly Type AssetNameType =
        typeof(IAssetName).Assembly.GetType("StardewModdingAPI.Framework.Content.AssetName")
        ?? throw new InvalidOperationException("SMAPI internal AssetName type not found");

    public static IAssetName Of(string name) {
        return (IAssetName) Activator.CreateInstance(AssetNameType, name, null, null)!;
    }
}
