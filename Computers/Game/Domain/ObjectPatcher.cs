using Computers.Core;
using StardewModdingAPI;
using StardewValley.GameData.Objects;

namespace Computers.Game.Domain;

public class ObjectPatcherService: IPatcherService {
    
    private readonly IMonitor _monitor;
    private readonly ISet<ContextEntry<ObjectData>> _objects;
    
    public ObjectPatcherService(IMonitor monitor, ISet<ContextEntry<ObjectData>> objects) {
        _monitor = monitor;
        _objects = objects;
        
        monitor.Log($"ObjectPatcherService initialized with {objects.Count} objects.", LogLevel.Debug);
    }
    
    public bool CanPatch(Type assetType, IAssetName assetName) {
        return assetType == typeof(Dictionary<string, ObjectData>) 
               && assetName.IsEquivalentTo("Data/Objects");
    }

    public void Patch(IAssetData asset) {
        _monitor.Log("Patching object data.", LogLevel.Debug);
        var assetData = asset.GetData<Dictionary<string, ObjectData>>();
        
        foreach (var obj in _objects) {
            assetData.Add(obj.Id.Value, obj.Value);
        }
    }
}
