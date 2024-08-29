using Computers.Computer;
using Computers.Computer.Boundary;
using Computers.Core;
using Computers.Game;
using Computers.Game.Boundary;
using Computers.Game.Utils;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using Object = StardewValley.Object;
using StardewModdingAPI;
using StardewValley.Menus;
using GameWindow = Computers.Game.Utils.GameWindow;

namespace Computers;

public class ModEntry : Mod {
    private static Core.Context _context = null!; // This will be initialized in Entry

    public override void Entry(IModHelper helper) {
        _context = Core.Context.Create(
            new IContextEntry.StatelessDataContextEntry(
                "tools.kot.nk2.computers.BigCraftable.Computer",
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = "tools.kot.nk2.computers.BigCraftable.Computer",
                    DisplayName = "Computer",
                    Description = "A computer.",
                    Price = 1000,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = "tools.kot.nk2.computers.TileSheet.Computer",
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                "tools.kot.nk2.computers.Recipe.Computer",
                typeof(Recipe),
                new Recipe(
                    "tools.kot.nk2.computers.Recipe.Computer",
                    "tools.kot.nk2.computers.BigCraftable.Computer",
                    new Dictionary<string, int> {
                        { "380", 5 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Computer"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                "tools.kot.nk2.computers.TileSheet.Computer",
                typeof(TileSheet),
                new TileSheet(
                    "tools.kot.nk2.computers.TileSheet.Computer",
                    "assets/Computer.png"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                "tools.kot.nk2.computers.TileSheet.Disk",
                typeof(TileSheet),
                new TileSheet(
                    "tools.kot.nk2.computers.TileSheet.Disk",
                    "assets/Disk.png"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                "tools.kot.nk2.computers.Item.Disk",
                typeof(ObjectData),
                new ObjectData {
                    Name = "tools.kot.nk2.computers.Item.Disk",
                    DisplayName = "Disk",
                    Description = "A disk.",
                    Price = 100,
                    Type = "Crafting",
                    Category = Object.CraftingCategory,
                    Texture = "tools.kot.nk2.computers.TileSheet.Disk",
                    SpriteIndex = 0
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                "tools.kot.nk2.computers.Machine.Computer",
                typeof(Machine),
                new Machine(
                    "(BC)tools.kot.nk2.computers.BigCraftable.Computer",
                    new MachineData {
                        HasInput = true,
                        HasOutput = true,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        LightWhileWorking = new MachineLight {
                            Color = "ForestGreen",
                            Radius = 5f
                        },
                        InteractMethod = "Computers.ModEntry, Computers: ComputerMachineInteractMethod",
                        OutputRules = new List<MachineOutputRule> {
                            new() {
                                Id = "tools.kot.nk2.computers.Machine.Computer.OutputRule",
                                Triggers = new List<MachineOutputTriggerRule> {
                                    new() {
                                        Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                                        RequiredItemId = "(O)tools.kot.nk2.computers.Item.Disk",
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
                "tools.kot.nk2.computers.Monitor",
                typeof(IMonitor),
                initializer => Monitor
            ),
            new IContextEntry.ServiceContextEntry(
                "tools.kot.nk2.computers.ModHelper",
                typeof(IModHelper),
                initializer => helper
            ),
            new IContextEntry.ServiceContextEntry(
                "tools.kot.nk2.computers.Service.BigCraftablePatchService",
                typeof(IPatcherService),
                initializer => new BigCraftablePatcherService(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Get<BigCraftableData>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                "tools.kot.nk2.computers.Service.TileSheetLoaderService",
                typeof(ILoaderService),
                initializer => new TileSheetLoader(
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<IModHelper>(),
                    initializer.Get<TileSheet>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                "tools.kot.nk2.computers.Service.RecipePatcherService",
                typeof(IPatcherService),
                initializer => new RecipePatcherService(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Get<Recipe>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                "tools.kot.nk2.computers.Service.MachinePatcherService",
                typeof(IPatcherService),
                initializer => new MachinePatcherService(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Get<Machine>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                "tools.kot.nk2.computers.Service.ObjectPatcherService",
                typeof(IPatcherService),
                initializer => new ObjectPatcherService(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Get<ObjectData>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                "tools.kot.nk2.computers.Service.AssetDispatcher",
                typeof(IEventHandler),
                initializer => new AssetDispatcher(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Get<ILoaderService>(),
                    initializer.Get<IPatcherService>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                "tools.kot.nk2.computers.Service.EventBus",
                typeof(IEventBus),
                initializer => new EventBus(
                    initializer.GetSingle<IMonitor>(),
                    initializer.Lookup<IEventHandler>()
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                "tools.kot.nk2.computers.Configuration",
                typeof(Configuration),
                helper.ModContent.Load<Configuration>("assets/Configuration.json")
            ),
            new IContextEntry.ServiceContextEntry(
                "tools.kot.nk2.computers.Service.ComputerTickDispatcher",
                typeof(IEventHandler),
                initializer => new ComputerTickDispatcher(
                    initializer.Lookup<IComputerPort>()
                )
            )
        );

        var (_, eventBus) = _context.Get<IEventBus>("tools.kot.nk2.computers.Service.EventBus");
        helper.Events.GameLoop.GameLaunched += (sender, e) => eventBus.Publish(new GameLaunchedEvent(e));
        helper.Events.Content.AssetRequested += (sender, e) => eventBus.Publish(new AssetRequestedEvent(e));
        helper.Events.GameLoop.UpdateTicked += (sender, e) => eventBus.Publish(new UpdateTickedEvent(e)); 
        // ObjectListChanged
    }

    private static void DrawScreen(IComputerPort computerPort) {
        var monitor = _context.GetSingle<IMonitor>("tools.kot.nk2.computers.Monitor");
        var configuration = _context.GetSingle<Configuration>("tools.kot.nk2.computers.Configuration");
        monitor.Log("Drawing screen.");
        
        // GameMenu
        Game1.activeClickableMenu = new GameWindow(
            configuration.WindowWidth,
            configuration.WindowHeight,
            (rectangle, batch) => computerPort.Fire(new RenderComputerEvent(rectangle, batch))
        );
    }

    public static bool ComputerMachineInteractMethod(
        Object machine, 
        GameLocation location,
        Farmer player
    ) {
        var monitor = _context.GetSingle<IMonitor>("tools.kot.nk2.computers.Monitor");
        monitor.Log($"Interacted with computer machine. Machine: {machine}, Location: {location}, Player: {player}, Held Object: {machine.heldObject}");

        var modData = machine.HeldObjectModData();
        if (modData == null) {
            monitor.Log("Computer does not have a held object - cannot infer script.");
            return false;
        }
        
        if (!modData.ContainsKey("ScriptId")) {
            monitor.Log("Computer does not have a script id - cannot infer script.");
            return false;
        }
        
        var scriptId = modData["ScriptId"];
        monitor.Log($"Computer has script id: {scriptId}");
        
        var computerState = _context.GetSingle<IComputerPort>(scriptId);
        
        DrawScreen(computerState);
        return true;
    }

    private static string MakeNewComputerState() {
        var configuration = _context.GetSingle<Configuration>("tools.kot.nk2.computers.Configuration");
        
        var uuid = Guid.NewGuid().ToString();
        var scriptId = $"tools.kot.nk2.computers.Script.{uuid}";

        var targetLoader = new TargetLoader<string>(
            _context.GetSingle<IModHelper>(),
            "assets/Library/Entrypoint.lua"
        );
        
        _context.Store(new ComputerStatefulDataContextEntry(
            scriptId,
            targetLoader,
            configuration
        ));
        
        return scriptId;
    }
    
    public static Item ComputerMachineOutputMethod(
        Object machine, 
        Item inputItem, 
        bool probe, 
        MachineItemOutput outputData, 
        out int? overrideMinutesUntilReady
    ) {
        var monitor = _context.GetSingle<IMonitor>("tools.kot.nk2.computers.Monitor");
        monitor.Log($"Preparing output from computer machine. Machine: {machine}, InputItem: {inputItem}, Probe: {probe}, OutputData: {outputData}");
        overrideMinutesUntilReady = null;

        var outputItem = inputItem.getOne();
        
        if (probe) {
            return outputItem;
        }

        if (outputItem.modData.ContainsKey("ScriptId")) {
            monitor.Log("ScriptId already exists.");
            return outputItem;
        }
        
        var scriptId = MakeNewComputerState();
        monitor.Log($"Setting ScriptId to {scriptId}");
        outputItem.modData["ScriptId"] = scriptId;

        return outputItem;
    }
}