using Computers.Computer;
using Computers.Computer.Domain;
using Computers.Core;
using Computers.Game;
using Computers.Game.Domain;
using Computers.Game.Utils;
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
    private static readonly Id ComputerBigCraftableId = GameBigCraftableBaseId / "Computer";
    private static readonly Id ComputerRecipeId = GameRecipeBaseId / "Computer";
    private static readonly Id ComputerTileSheetId = GameTileSheetBaseId / "Computer";
    private static readonly Id DiskTileSheetId = GameTileSheetBaseId / "Disk";
    private static readonly Id DiskItemId = GameItemBaseId / "Disk";
    private static readonly Id DiskRecipeId = GameRecipeBaseId / "Disk";
    private static readonly Id ComputerMachineId = GameMachineBaseId / "Computer";
    private static readonly Id ComputerMachineOutputRuleId = ComputerMachineId / "OutputRule";
    
    public override void Entry(IModHelper helper) {
        _context = Core.Context.Create(
            new IContextEntry.StatelessDataContextEntry(
                ComputerBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = ComputerBigCraftableId.Value,
                    DisplayName = "Computer",
                    Description = "A computer.",
                    Price = 1000,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = ComputerTileSheetId.Value,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                ComputerRecipeId,
                typeof(Recipe),
                new Recipe(
                    ComputerBigCraftableId.Value,
                    new Dictionary<string, int> {
                        { "380", 5 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Computer"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                ComputerTileSheetId,
                typeof(TileSheet),
                new TileSheet(ComputerTileSheetId.Value, "assets/Computer.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                DiskTileSheetId,
                typeof(TileSheet),
                new TileSheet(DiskTileSheetId.Value, "assets/Disk.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                DiskItemId,
                typeof(ObjectData),
                new ObjectData {
                    Name = DiskItemId.Value,
                    DisplayName = "Disk",
                    Description = "A disk.",
                    Price = 100,
                    Type = "Crafting",
                    Category = Object.CraftingCategory,
                    Texture = DiskTileSheetId.Value,
                    SpriteIndex = 0,
                    CanBeGivenAsGift = false
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                DiskRecipeId,
                typeof(Recipe),
                new Recipe(
                    DiskItemId.Value,
                    new Dictionary<string, int> {
                        { "380", 1 }
                    },
                    false,
                    new IRecipeRequirement.NoneRequired(),
                    "Disk"
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
                                Id = ComputerMachineOutputRuleId.Value,
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
                ServiceBaseId / "ComputerFactory",
                typeof(IStatefulDataContextEntryFactory),
                initializer => new ComputerStatefulDataContextEntryFactory(
                    ServiceBaseId / "ComputerFactory",
                    ComputerBaseId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<IRedundantLoader>(ServiceBaseId / "RedundantLoader"),
                    initializer.GetSingle<IRedundantLoader>(ServiceBaseId / "AssetsLoader")
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ComputerStartDispatcher",
                typeof(IEventHandler),
                initializer => new ComputerStartDispatcher(
                    initializer.Lookup<IComputerPort>()
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
            outputItem.modData["ComputerId"] = computer.Id.Value;
        }
        
        computer.Start();
        return outputItem;
    }

    private static void HandleSave() {
        var helper = _context.GetSingle<IModHelper>(ServiceBaseId / "ModHelper");
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        
        monitor.Log("Saving data.");
        var data = _context.Store();
        helper.Data.WriteSaveData("tools.kot.nk2.computers", data.Serialize());
    }
    
    private static void HandleLoad(SaveLoadedEventArgs args) {
        var helper = _context.GetSingle<IModHelper>(ServiceBaseId / "ModHelper");
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        var eventBus = _context.GetSingle<IEventBus>(ServiceBaseId / "EventBus");
        
        var data = helper.Data.ReadSaveData<string>("tools.kot.nk2.computers");
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
        foreach(var (position, obj) in args.Removed) {
            monitor.Log($"Removed object at {position}: {obj}");
            if (obj.heldObject.Value is null) {
                monitor.Log("Object does not have a held object.");
                continue;
            }

            var modData = obj.HeldObjectModData();
            if (modData is null || !modData.ContainsKey("ComputerId")) {
                monitor.Log("Object does not have a computer id.");
                continue;
            }
            
            monitor.Log($"Stopping computer with id {modData["ComputerId"]}");
            var computerId = modData["ComputerId"].AsId();
            
            // Stop computer
            var computerState = _context.GetSingle<IComputerPort>(computerId);
            computerState.Fire(new StopComputerEvent(computerState.Id));
            
            // Make a disk with the script
            Game1.createItemDebris(
                obj.heldObject.Value,
                position * Game1.tileSize,
                Game1.random.Next(4),
                Game1.player.currentLocation
            );
        }
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
