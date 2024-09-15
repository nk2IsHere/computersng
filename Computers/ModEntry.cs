using Computers.Computer;
using Computers.Computer.Domain;
using Computers.Computer.Domain.Event;
using Computers.Core;
using Computers.Game;
using Computers.Game.Domain;
using Computers.Game.Utils;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Router.Domain.Event;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using Object = StardewValley.Object;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using GameWindow = Computers.Game.Utils.GameWindow;

namespace Computers;

public class ModEntry : Mod {
    private static Core.Context _context = null!; // This will be initialized in Entry
    
    // Base Id
    private static readonly Id BaseId = "tools.kot.nk2.computers".AsId();
    
    // Game Base Id
    private static readonly Id GameBigCraftableBaseId = BaseId / "BigCraftable";
    private static readonly Id GameRecipeBaseId = BaseId / "Recipe";
    private static readonly Id GameTileSheetBaseId = BaseId / "TileSheet";
    private static readonly Id GameItemBaseId = BaseId / "Item";
    private static readonly Id GameMachineBaseId = BaseId / "Machine";
    
    // Service Base Id
    private static readonly Id ServiceBaseId = BaseId / "Service";
    
    // Computer-related Base Id
    private static readonly Id ComputerBaseId = BaseId / "Data" / "Computer";
    
    // Game-related Ids
    private static readonly Id ComputerTileSheetId = GameTileSheetBaseId / "Computer";
    private static readonly Id ComputerBigCraftableId = GameBigCraftableBaseId / "Computer";
    private static readonly Id ComputerRecipeId = GameRecipeBaseId / "Computer";
    
    private static readonly Id ComputerMachineId = GameMachineBaseId / "Computer";
    private static readonly Id ComputerMachineOutputRuleId = ComputerMachineId / "OutputRule";

    private static readonly Id DiskTileSheetId = GameTileSheetBaseId / "Disk";
    private static readonly Id DiskItemId = GameItemBaseId / "Disk";
    private static readonly Id DiskRecipeId = GameRecipeBaseId / "Disk";
    
    private static readonly Id RouterTileSheetId = GameTileSheetBaseId / "Router";
    private static readonly Id RouterBigCraftableId = GameBigCraftableBaseId / "Router";
    private static readonly Id RouterRecipeId = GameRecipeBaseId / "Router";
    
    private static readonly Id RouterMachineId = GameMachineBaseId / "Router";
    
    public override void Entry(IModHelper helper) {
        _context = Core.Context.Create(
            new IContextEntry.StatelessDataContextEntry(
                ComputerTileSheetId,
                typeof(TileSheet),
                new TileSheet(ComputerTileSheetId, "assets/Computer.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                ComputerBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = ComputerBigCraftableId,
                    DisplayName = "Computer",
                    Description = "A computer.",
                    Price = 1000,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = ComputerTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                ComputerRecipeId,
                typeof(Recipe),
                new Recipe(
                    ComputerBigCraftableId,
                    new Dictionary<string, int> {
                        { "380", 5 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Computer"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                ComputerMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){ComputerBigCraftableId}",
                    new MachineData {
                        HasInput = true,
                        HasOutput = true,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        LightWhileWorking = new MachineLight {
                            Color = "ForestGreen",
                            Radius = 2f
                        },
                        InteractMethod = "Computers.ModEntry, Computers: ComputerMachineInteractMethod",
                        OutputRules = new List<MachineOutputRule> {
                            new() {
                                Id = ComputerMachineOutputRuleId,
                                Triggers = new List<MachineOutputTriggerRule> {
                                    new() {
                                        Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                                        RequiredItemId = $"(O){DiskItemId}",
                                        RequiredCount = 1
                                    }
                                },
                                OutputItem = new List<MachineItemOutput> {
                                    new() {
                                        OutputMethod = "Computers.ModEntry, Computers: ComputerMachineOutputMethod"
                                    }
                                },
                                UseFirstValidOutput = true,
                                // "Infinite" ready time (cannot use max value since some calculations will overflow)
                                MinutesUntilReady = int.MaxValue / 2
                            }
                        }
                    }
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                DiskTileSheetId,
                typeof(TileSheet),
                new TileSheet(DiskTileSheetId, "assets/Disk.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                DiskItemId,
                typeof(ObjectData),
                new ObjectData {
                    Name = DiskItemId,
                    DisplayName = "Disk",
                    Description = "A disk.",
                    Price = 100,
                    Type = "Crafting",
                    Category = Object.CraftingCategory,
                    Texture = DiskTileSheetId,
                    SpriteIndex = 0,
                    CanBeGivenAsGift = false
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                DiskRecipeId,
                typeof(Recipe),
                new Recipe(
                    DiskItemId,
                    new Dictionary<string, int> {
                        { "380", 1 }
                    },
                    false,
                    new IRecipeRequirement.NoneRequired(),
                    "Disk"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                RouterTileSheetId,
                typeof(TileSheet),
                new TileSheet(RouterTileSheetId, "assets/Router.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                RouterBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = RouterBigCraftableId,
                    DisplayName = "Router",
                    Description = "A router.",
                    Price = 1000,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = RouterTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                RouterRecipeId,
                typeof(Recipe),
                new Recipe(
                    RouterBigCraftableId,
                    new Dictionary<string, int> {
                        { "380", 1 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Router"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                RouterMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){RouterBigCraftableId}",
                    new MachineData {
                        HasInput = false,
                        HasOutput = false,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        InteractMethod = "Computers.ModEntry, Computers: RouterMachineInteractMethod",
                    }
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "Monitor",
                typeof(IMonitor),
                _ => Monitor
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ModHelper",
                typeof(IModHelper),
                _ => helper
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "BigCraftablePatcherService",
                typeof(IPatcherService),
                initializer => new BigCraftablePatcherService(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Get<BigCraftableData>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "TileSheetLoaderService",
                typeof(ILoaderService),
                initializer => new TileSheetLoader(
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<IModHelper>(),
                    initializer.Get<TileSheet>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "RecipePatcherService",
                typeof(IPatcherService),
                initializer => new RecipePatcherService(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Get<Recipe>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "MachinePatcherService",
                typeof(IPatcherService),
                initializer => new MachinePatcherService(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Get<Machine>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ObjectPatcherService",
                typeof(IPatcherService),
                initializer => new ObjectPatcherService(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Get<ObjectData>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "AssetDispatcher",
                typeof(IEventHandler),
                initializer => new AssetDispatcher(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Get<ILoaderService>(),
                    initializer.Get<IPatcherService>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "EventBus",
                typeof(IEventBus),
                initializer => new EventBus(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Lookup<IEventHandler>()
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                ServiceBaseId / "Configuration",
                typeof(Configuration),
                helper
                    .LoadString("assets/Configuration.yml")
                    .DeserializeConfiguration<Configuration>()
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ComputerTickDispatcher",
                typeof(IEventHandler),
                initializer => new ComputerTickDispatcher(
                    initializer.Lookup<IComputerPort>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ComputerStopDispatcher",
                typeof(IEventHandler),
                initializer => new ComputerStopDispatcher(
                    initializer.Lookup<IComputerPort>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ComputerButtonDispatcher",
                typeof(IEventHandler),
                initializer => new ComputerButtonDispatcher(
                    initializer.Lookup<IComputerPort>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "RedundantLoader",
                typeof(IRedundantLoader),
                initializer => new RedundantLoader(
                    initializer.GetSingle<IModHelper>(),
                    $"assets/{initializer.GetSingle<Configuration>().Resource.CoreLibraryPath}"
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "AssetsLoader",
                typeof(IRedundantLoader),
                initializer => new RedundantLoader(
                    initializer.GetSingle<IModHelper>(),
                    $"assets/{initializer.GetSingle<Configuration>().Resource.AssetsPath}"
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "DataLoader",
                typeof(IRedundantLoader),
                initializer => new RedundantLoader(
                    initializer.GetSingle<IModHelper>(),
                    initializer.GetSingle<Configuration>().Storage.ExternalStorageFolder
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ComputerFactory",
                typeof(IStatefulDataContextEntryFactory),
                initializer => new ComputerStatefulDataContextEntryFactory(
                    ServiceBaseId / "ComputerFactory",
                    ComputerBaseId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<IRedundantLoader>(ServiceBaseId / "RedundantLoader"),
                    initializer.GetSingle<IRedundantLoader>(ServiceBaseId / "AssetsLoader"),
                    initializer.GetSingle<IRedundantLoader>(ServiceBaseId / "DataLoader")
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ComputerStartDispatcher",
                typeof(IEventHandler),
                initializer => new ComputerStartDispatcher(
                    initializer.Lookup<IComputerPort>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "RouterFactory",
                typeof(IStatefulDataContextEntryFactory),
                initializer => new RouterStatefulDataContextEntryFactory(
                    ServiceBaseId / "RouterFactory",
                    RouterBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "RouterStartDispatcher",
                typeof(IEventHandler),
                initializer => new RouterStartDispatcher(
                    initializer.Lookup<IRouterPort>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "RouterStopDispatcher",
                typeof(IEventHandler),
                initializer => new RouterStopDispatcher(
                    initializer.Lookup<IRouterPort>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "RouterTickDispatcher",
                typeof(IEventHandler),
                initializer => new RouterTickDispatcher(
                    initializer.Lookup<IRouterPort>()
                )
            )
        );

        var eventBus = _context.GetSingle<IEventBus>(ServiceBaseId / "EventBus");
        
        helper.Events.Content.AssetRequested += (_, e) => eventBus.Publish(new AssetRequestedEvent(e));
        
        helper.Events.GameLoop.GameLaunched += (_, e) => eventBus.Publish(new GameLaunchedEvent(e));
        helper.Events.GameLoop.UpdateTicked += (_, e) => eventBus.Publish(new UpdateTickedEvent(e));
        helper.Events.GameLoop.ReturnedToTitle += (_, e) => eventBus.Publish(new ReturnedToTitleEvent(e));
        
        helper.Events.Input.ButtonPressed += (_, e) => eventBus.Publish(new ButtonPressedEvent(e));
        helper.Events.Input.ButtonReleased += (_, e) => eventBus.Publish(new ButtonReleasedEvent(e));
        
        helper.Events.World.ObjectListChanged += (_, e) => HandleObjectListChanged(e);
        helper.Events.GameLoop.SaveCreating += (_, _) => HandleSave();
        helper.Events.GameLoop.Saving += (_, _) => HandleSave();
        helper.Events.GameLoop.SaveLoaded += (_, e) => HandleLoad(e);
    }

    public static bool ComputerMachineInteractMethod(
        Object machine, 
        GameLocation location,
        Farmer player
    ) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log($"Interacted with computer machine. Machine: {machine}, Location: {location}, Player: {player}, Held Object: {machine.heldObject}");

        var modData = machine.HeldObjectModData();
        if (modData == null) {
            monitor.Log("Computer does not have a held object - cannot infer script.");
            return false;
        }
        
        if (!modData.ContainsKey("ComputerId")) {
            monitor.Log("Computer does not have a script id - cannot infer script.");
            return false;
        }
        
        var computerId = modData["ComputerId"].AsId();
        monitor.Log($"Computer has id: {computerId}");
        
        var computerState = _context.GetSingle<IComputerPort>(computerId);
        DrawScreen(computerState);
        return true;
    }
    
    public static Item ComputerMachineOutputMethod(
        Object machine, 
        Item inputItem, 
        bool probe, 
        MachineItemOutput outputData, 
        out int? overrideMinutesUntilReady
    ) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log($"Preparing output from computer machine. Machine: {machine}, InputItem: {inputItem}, Probe: {probe}, OutputData: {outputData}");
        
        overrideMinutesUntilReady = null;
        var outputItem = inputItem.getOne();
        
        if (probe) {
            return outputItem;
        }

        IComputerPort computer;
        if (outputItem.modData.ContainsKey("ComputerId")) {
            monitor.Log("ComputerId already exists.");
            computer = _context.GetSingle<IComputerPort>(outputItem.modData["ComputerId"].AsId());
        }
        else {
            computer = _context.ProduceSingle<ComputerStatefulDataContextEntry>(ServiceBaseId / "ComputerFactory");
            monitor.Log($"Setting ComputerId to {computer.Id}");
            outputItem.modData["ComputerId"] = computer.Id;
        }
        
        computer.Start();
        return outputItem;
    }
    
    public static bool RouterMachineInteractMethod(
        Object machine, 
        GameLocation location,
        Farmer player
    ) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log($"Interacted with router machine. Machine: {machine}, Location: {location}, Player: {player}, Held Object: {machine.heldObject}");
        var modData = machine.modData;
        if (modData == null) {
            monitor.Log("Router does not have mod data - cannot infer id.");
            return false;
        }
        
        if (!modData.ContainsKey("RouterId")) {
            monitor.Log("Router does not have an id - cannot show it.");
            return false;
        }
        
        var routerId = modData["RouterId"].AsId();
        monitor.Log($"Router has id: {routerId}");
        
        // Show router id in a message box
        Game1.showGlobalMessage($"Router Id: {routerId.Last}");
        return true;
    }

    private static void HandleSave() {
        var helper = _context.GetSingle<IModHelper>(ServiceBaseId / "ModHelper");
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        
        monitor.Log("Saving data.");
        var data = _context.Store();
        helper.Data.WriteSaveData(BaseId, data.Serialize());
    }
    
    private static void HandleLoad(SaveLoadedEventArgs args) {
        var helper = _context.GetSingle<IModHelper>(ServiceBaseId / "ModHelper");
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        var eventBus = _context.GetSingle<IEventBus>(ServiceBaseId / "EventBus");
        
        var data = helper.Data.ReadSaveData<string>(BaseId);
        if (data is null) {
            monitor.Log("No data found.");
            return;
        }

        var deserializedData = data.Deserialize<Dictionary<Id, Dictionary<string, object>>>();
        if (deserializedData is null) {
            monitor.Log("Data could not be deserialized.");
            return;
        }
        
        monitor.Log("Loading data.");
        _context.Restore(deserializedData);
        eventBus.Publish(new SaveLoadedEvent(args));
    }

    private static void HandleObjectListChanged(ObjectListChangedEventArgs args) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");

        foreach (var (position, obj) in args.Added) {
            monitor.Log($"Added object at {position}: {obj}");

            if (obj.ItemId == RouterBigCraftableId) {
                HandleRouterAdded(obj, position);
            }
        }
        
        foreach(var (position, obj) in args.Removed) {
            monitor.Log($"Removed object at {position}: {obj}");

            var objectData = obj.modData;
            var heldObjectModData = obj.HeldObjectModData();
            
            if (heldObjectModData is not null && heldObjectModData.ContainsKey("ComputerId")) {
                HandleComputerRemoved(heldObjectModData["ComputerId"].AsId(), obj, position);
            }
            
            if (objectData is not null && objectData.ContainsKey("RouterId")) {
                HandleRouterRemoved(objectData["RouterId"].AsId(), obj, position);
            }
        }
    }

    private static void HandleRouterAdded(Object obj, Vector2 position) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log("Router added.");

        IRouterPort router;
        if (obj.modData.ContainsKey("RouterId")) {
            monitor.Log("RouterId already exists.");
            router = _context.GetSingle<IRouterPort>(obj.modData["RouterId"].AsId());
        } else {
            router = _context.ProduceSingle<RouterStatefulDataContextEntry>(ServiceBaseId / "RouterFactory");
            monitor.Log($"Setting RouterId to {router.Id}");
            obj.modData["RouterId"] = router.Id;
        }

        router.Start();
    }
    
    private static void HandleRouterRemoved(Id routerId, Object obj, Vector2 position) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log($"Router with id {routerId} was removed.");
        
        var router = _context.GetSingle<IRouterPort>(routerId);
        router.Stop();
    }
    
    private static void HandleComputerRemoved(Id computerId, Object obj, Vector2 position) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        var heldObject = obj.heldObject.Value;
        
        if (heldObject is null) {
            monitor.Log("Object does not have a held object.");
            return;
        }
        
        monitor.Log($"Computer with id {computerId} was removed.");
        
        // Stop computer
        var computerState = _context.GetSingle<IComputerPort>(computerId);
        computerState.Fire(new StopComputerEvent(computerState.Id));
            
        // Make a disk with the script
        Game1.createItemDebris(
            heldObject,
            position * Game1.tileSize,
            Game1.random.Next(4),
            Game1.player.currentLocation
        );
    }
    
    private static void DrawScreen(IComputerPort computerPort) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        var configuration = _context.GetSingle<Configuration>(ServiceBaseId / "Configuration");
        monitor.Log("Drawing screen.");
        
        Game1.InUIMode(() => {
            Game1.activeClickableMenu = new GameWindow(
                configuration.Ui.WindowWidth,
                configuration.Ui.WindowHeight,
                (rectangle, batch) => computerPort.Fire(new RenderComputerEvent(rectangle, batch)),
                onReceiveLeftClick: (x, y) => computerPort.Fire(new MouseLeftClickedEvent(computerPort.Id, x, y)),
                onReceiveRightClick: (x, y) => computerPort.Fire(new MouseRightClickedEvent(computerPort.Id, x, y)),
                onReceiveKeyPress: key => computerPort.Fire(new KeyPressedEvent(computerPort.Id, key)),
                onReceiveScrollWheelAction: direction => computerPort.Fire(new MouseWheelEvent(computerPort.Id, direction))
            );
        });
    }
}
