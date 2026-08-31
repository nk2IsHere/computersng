using Computers.Computer;
using Computers.Computer.Domain;
using Computers.Computer.Domain.Event;
using Computers.Core;
using Computers.Game;
using Computers.Game.Domain;
using Computers.Game.Patch;
using Computers.Game.Utils;
using Computers.MachineController;
using Computers.MachineController.Domain;
using Computers.Mailer;
using Computers.Mailer.Domain;
using Computers.Peripheral;
using Computers.PlayerSensor;
using Computers.PlayerSensor.Domain;
using Computers.Peripheral.Domain.Event;
using Computers.Router;
using Computers.Router.Domain;
using Computers.ShippingController;
using Computers.ShippingController.Domain;
using Computers.Speaker;
using Computers.Speaker.Domain;
using Computers.Router.Domain.Event;
using Computers.WeatherStation;
using Computers.WeatherStation.Domain;
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

    // The e2e harness mod resolves entities such as computers through the shared context.
    internal static Core.Context SharedContext => _context;
    
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
    internal static readonly Id ComputerBigCraftableId = GameBigCraftableBaseId / "Computer";
    private static readonly Id ComputerRecipeId = GameRecipeBaseId / "Computer";
    
    private static readonly Id ComputerMachineId = GameMachineBaseId / "Computer";
    private static readonly Id ComputerMachineOutputRuleId = ComputerMachineId / "OutputRule";

    private static readonly Id DiskTileSheetId = GameTileSheetBaseId / "Disk";
    internal static readonly Id DiskItemId = GameItemBaseId / "Disk";
    private static readonly Id DiskRecipeId = GameRecipeBaseId / "Disk";
    
    private static readonly Id RouterTileSheetId = GameTileSheetBaseId / "Router";
    internal static readonly Id RouterBigCraftableId = GameBigCraftableBaseId / "Router";
    private static readonly Id RouterRecipeId = GameRecipeBaseId / "Router";
    
    private static readonly Id RouterMachineId = GameMachineBaseId / "Router";

    private static readonly Id MachineControllerTileSheetId = GameTileSheetBaseId / "MachineController";

    private static readonly Id WeatherStationTileSheetId = GameTileSheetBaseId / "WeatherStation";
    internal static readonly Id WeatherStationBigCraftableId = GameBigCraftableBaseId / "WeatherStation";
    private static readonly Id WeatherStationRecipeId = GameRecipeBaseId / "WeatherStation";
    private static readonly Id WeatherStationMachineId = GameMachineBaseId / "WeatherStation";

    private static readonly Id PlayerSensorTileSheetId = GameTileSheetBaseId / "PlayerSensor";

    private static readonly Id MailerTileSheetId = GameTileSheetBaseId / "Mailer";
    internal static readonly Id MailerBigCraftableId = GameBigCraftableBaseId / "Mailer";
    private static readonly Id MailerRecipeId = GameRecipeBaseId / "Mailer";
    private static readonly Id MailerMachineId = GameMachineBaseId / "Mailer";

    private static readonly Id ShippingControllerTileSheetId = GameTileSheetBaseId / "ShippingController";
    internal static readonly Id ShippingControllerBigCraftableId = GameBigCraftableBaseId / "ShippingController";
    private static readonly Id ShippingControllerRecipeId = GameRecipeBaseId / "ShippingController";
    private static readonly Id ShippingControllerMachineId = GameMachineBaseId / "ShippingController";

    private static readonly Id SpeakerTileSheetId = GameTileSheetBaseId / "Speaker";
    internal static readonly Id SpeakerBigCraftableId = GameBigCraftableBaseId / "Speaker";
    private static readonly Id SpeakerRecipeId = GameRecipeBaseId / "Speaker";
    private static readonly Id SpeakerMachineId = GameMachineBaseId / "Speaker";
    internal static readonly Id PlayerSensorBigCraftableId = GameBigCraftableBaseId / "PlayerSensor";
    private static readonly Id PlayerSensorRecipeId = GameRecipeBaseId / "PlayerSensor";
    private static readonly Id PlayerSensorMachineId = GameMachineBaseId / "PlayerSensor";
    internal static readonly Id MachineControllerBigCraftableId = GameBigCraftableBaseId / "MachineController";
    private static readonly Id MachineControllerRecipeId = GameRecipeBaseId / "MachineController";

    private static readonly Id AdvancedMachineControllerTileSheetId = GameTileSheetBaseId / "AdvancedMachineController";
    internal static readonly Id AdvancedMachineControllerBigCraftableId = GameBigCraftableBaseId / "AdvancedMachineController";
    private static readonly Id AdvancedMachineControllerRecipeId = GameRecipeBaseId / "AdvancedMachineController";
    private static readonly Id AdvancedMachineControllerMachineId = GameMachineBaseId / "AdvancedMachineController";

    private static readonly Id AdvancedShippingControllerTileSheetId = GameTileSheetBaseId / "AdvancedShippingController";
    internal static readonly Id AdvancedShippingControllerBigCraftableId = GameBigCraftableBaseId / "AdvancedShippingController";
    private static readonly Id AdvancedShippingControllerRecipeId = GameRecipeBaseId / "AdvancedShippingController";
    private static readonly Id AdvancedShippingControllerMachineId = GameMachineBaseId / "AdvancedShippingController";

    private static readonly Id AdvancedPlayerSensorTileSheetId = GameTileSheetBaseId / "AdvancedPlayerSensor";
    internal static readonly Id AdvancedPlayerSensorBigCraftableId = GameBigCraftableBaseId / "AdvancedPlayerSensor";
    private static readonly Id AdvancedPlayerSensorRecipeId = GameRecipeBaseId / "AdvancedPlayerSensor";
    private static readonly Id AdvancedPlayerSensorMachineId = GameMachineBaseId / "AdvancedPlayerSensor";

    private static readonly Id AdvancedRouterTileSheetId = GameTileSheetBaseId / "AdvancedRouter";
    internal static readonly Id AdvancedRouterBigCraftableId = GameBigCraftableBaseId / "AdvancedRouter";
    private static readonly Id AdvancedRouterRecipeId = GameRecipeBaseId / "AdvancedRouter";
    private static readonly Id AdvancedRouterMachineId = GameMachineBaseId / "AdvancedRouter";

    private static readonly Id MachineControllerMachineId = GameMachineBaseId / "MachineController";
    
    public override void Entry(IModHelper helper) {
        ItemIdentityPatches.Apply(BaseId);

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
                    Description = "A programmable computer. Insert a disk and it boots a JavaScript console you can script.",
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
                    Description = "Holds a computer's identity and files. The computer that boots it remembers everything.",
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
                    Description = "Connects nearby computers and peripherals into a network. Routers mesh with each other and matching channels bridge locations.",
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
            new IContextEntry.StatelessDataContextEntry(
                MachineControllerTileSheetId,
                typeof(TileSheet),
                new TileSheet(MachineControllerTileSheetId, "assets/MachineController.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                MachineControllerBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = MachineControllerBigCraftableId,
                    DisplayName = "Machine Controller",
                    Description = "Controls an edge-connected group of machines and chests over the network.",
                    Price = 1000,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = MachineControllerTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                MachineControllerRecipeId,
                typeof(Recipe),
                new Recipe(
                    MachineControllerBigCraftableId,
                    new Dictionary<string, int> {
                        { "380", 5 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Machine Controller"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                MachineControllerMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){MachineControllerBigCraftableId}",
                    new MachineData {
                        HasInput = false,
                        HasOutput = false,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        InteractMethod = "Computers.ModEntry, Computers: PeripheralMachineInteractMethod",
                    }
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                WeatherStationTileSheetId,
                typeof(TileSheet),
                new TileSheet(WeatherStationTileSheetId, "assets/WeatherStation.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                WeatherStationBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = WeatherStationBigCraftableId,
                    DisplayName = "Weather Station",
                    Description = "Reads time, date, weather and luck over the network.",
                    Price = 500,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = WeatherStationTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                WeatherStationRecipeId,
                typeof(Recipe),
                new Recipe(
                    WeatherStationBigCraftableId,
                    new Dictionary<string, int> {
                        { "787", 1 },
                        { "338", 2 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Weather Station"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                WeatherStationMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){WeatherStationBigCraftableId}",
                    new MachineData {
                        HasInput = false,
                        HasOutput = false,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        InteractMethod = "Computers.ModEntry, Computers: PeripheralMachineInteractMethod",
                    }
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                PlayerSensorTileSheetId,
                typeof(TileSheet),
                new TileSheet(PlayerSensorTileSheetId, "assets/PlayerSensor.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                PlayerSensorBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = PlayerSensorBigCraftableId,
                    DisplayName = "Player Sensor",
                    Description = "Detects players and NPCs nearby and reports them over the network.",
                    Price = 500,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = PlayerSensorTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                PlayerSensorRecipeId,
                typeof(Recipe),
                new Recipe(
                    PlayerSensorBigCraftableId,
                    new Dictionary<string, int> {
                        { "787", 1 },
                        { "336", 1 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Player Sensor"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                PlayerSensorMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){PlayerSensorBigCraftableId}",
                    new MachineData {
                        HasInput = false,
                        HasOutput = false,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        InteractMethod = "Computers.ModEntry, Computers: PeripheralMachineInteractMethod",
                    }
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                MailerTileSheetId,
                typeof(TileSheet),
                new TileSheet(MailerTileSheetId, "assets/Mailer.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                MailerBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = MailerBigCraftableId,
                    DisplayName = "Mailer",
                    Description = "Shows notifications and sends in-game mail over the network.",
                    Price = 500,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = MailerTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                MailerRecipeId,
                typeof(Recipe),
                new Recipe(
                    MailerBigCraftableId,
                    new Dictionary<string, int> {
                        { "787", 1 },
                        { "388", 25 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Mailer"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                MailerMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){MailerBigCraftableId}",
                    new MachineData {
                        HasInput = false,
                        HasOutput = false,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        InteractMethod = "Computers.ModEntry, Computers: PeripheralMachineInteractMethod",
                    }
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                ShippingControllerTileSheetId,
                typeof(TileSheet),
                new TileSheet(ShippingControllerTileSheetId, "assets/ShippingController.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                ShippingControllerBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = ShippingControllerBigCraftableId,
                    DisplayName = "Shipping Controller",
                    Description = "Sells items from adjacent chests through the shipping bin over the network.",
                    Price = 800,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = ShippingControllerTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                ShippingControllerRecipeId,
                typeof(Recipe),
                new Recipe(
                    ShippingControllerBigCraftableId,
                    new Dictionary<string, int> {
                        { "787", 1 },
                        { "335", 2 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Shipping Controller"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                ShippingControllerMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){ShippingControllerBigCraftableId}",
                    new MachineData {
                        HasInput = false,
                        HasOutput = false,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        InteractMethod = "Computers.ModEntry, Computers: PeripheralMachineInteractMethod",
                    }
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                SpeakerTileSheetId,
                typeof(TileSheet),
                new TileSheet(SpeakerTileSheetId, "assets/Speaker.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                SpeakerBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = SpeakerBigCraftableId,
                    DisplayName = "Speaker",
                    Description = "Plays game sound cues over the network.",
                    Price = 400,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = SpeakerTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                SpeakerRecipeId,
                typeof(Recipe),
                new Recipe(
                    SpeakerBigCraftableId,
                    new Dictionary<string, int> {
                        { "787", 1 },
                        { "338", 1 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Speaker"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                SpeakerMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){SpeakerBigCraftableId}",
                    new MachineData {
                        HasInput = false,
                        HasOutput = false,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        InteractMethod = "Computers.ModEntry, Computers: PeripheralMachineInteractMethod",
                    }
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedMachineControllerTileSheetId,
                typeof(TileSheet),
                new TileSheet(AdvancedMachineControllerTileSheetId, "assets/AdvancedMachineController.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedMachineControllerBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = AdvancedMachineControllerBigCraftableId,
                    DisplayName = "Advanced Machine Controller",
                    Description = "Controls an edge-connected group of machines and chests over the network. Reaches the whole connected group.",
                    Price = 2000,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = AdvancedMachineControllerTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedMachineControllerRecipeId,
                typeof(Recipe),
                new Recipe(
                    AdvancedMachineControllerBigCraftableId,
                    new Dictionary<string, int> {
                        { "380", 5 },
                        { "337", 1 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Advanced Machine Controller"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedMachineControllerMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){AdvancedMachineControllerBigCraftableId}",
                    new MachineData {
                        HasInput = false,
                        HasOutput = false,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        InteractMethod = "Computers.ModEntry, Computers: PeripheralMachineInteractMethod",
                    }
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedShippingControllerTileSheetId,
                typeof(TileSheet),
                new TileSheet(AdvancedShippingControllerTileSheetId, "assets/AdvancedShippingController.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedShippingControllerBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = AdvancedShippingControllerBigCraftableId,
                    DisplayName = "Advanced Shipping Controller",
                    Description = "Sells items from the whole connected chest group through the shipping bin over the network.",
                    Price = 1600,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = AdvancedShippingControllerTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedShippingControllerRecipeId,
                typeof(Recipe),
                new Recipe(
                    AdvancedShippingControllerBigCraftableId,
                    new Dictionary<string, int> {
                        { "787", 1 },
                        { "335", 2 },
                        { "337", 1 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Advanced Shipping Controller"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedShippingControllerMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){AdvancedShippingControllerBigCraftableId}",
                    new MachineData {
                        HasInput = false,
                        HasOutput = false,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        InteractMethod = "Computers.ModEntry, Computers: PeripheralMachineInteractMethod",
                    }
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedPlayerSensorTileSheetId,
                typeof(TileSheet),
                new TileSheet(AdvancedPlayerSensorTileSheetId, "assets/AdvancedPlayerSensor.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedPlayerSensorBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = AdvancedPlayerSensorBigCraftableId,
                    DisplayName = "Advanced Player Sensor",
                    Description = "Detects players and NPCs far around and reports them over the network.",
                    Price = 1000,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = AdvancedPlayerSensorTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedPlayerSensorRecipeId,
                typeof(Recipe),
                new Recipe(
                    AdvancedPlayerSensorBigCraftableId,
                    new Dictionary<string, int> {
                        { "787", 1 },
                        { "336", 1 },
                        { "337", 1 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Advanced Player Sensor"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedPlayerSensorMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){AdvancedPlayerSensorBigCraftableId}",
                    new MachineData {
                        HasInput = false,
                        HasOutput = false,
                        AllowFairyDust = false,
                        WobbleWhileWorking = false,
                        InteractMethod = "Computers.ModEntry, Computers: PeripheralMachineInteractMethod",
                    }
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedRouterTileSheetId,
                typeof(TileSheet),
                new TileSheet(AdvancedRouterTileSheetId, "assets/AdvancedRouter.png")
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedRouterBigCraftableId,
                typeof(BigCraftableData),
                new BigCraftableData {
                    Name = AdvancedRouterBigCraftableId,
                    DisplayName = "Advanced Router",
                    Description = "Connects computers and peripherals into a network. Bridges locations by channel.",
                    Price = 2000,
                    Fragility = 0,
                    CanBePlacedOutdoors = true,
                    CanBePlacedIndoors = true,
                    IsLamp = false,
                    Texture = AdvancedRouterTileSheetId,
                    SpriteIndex = 0,
                    ContextTags = null,
                    CustomFields = null
                }
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedRouterRecipeId,
                typeof(Recipe),
                new Recipe(
                    AdvancedRouterBigCraftableId,
                    new Dictionary<string, int> {
                        { "787", 1 },
                        { "337", 1 }
                    },
                    true,
                    new IRecipeRequirement.NoneRequired(),
                    "Advanced Router"
                )
            ),
            new IContextEntry.StatelessDataContextEntry(
                AdvancedRouterMachineId,
                typeof(Machine),
                new Machine(
                    $"(BC){AdvancedRouterBigCraftableId}",
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
                ServiceBaseId / "Random",
                typeof(Random),
                _ => Game1.random
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
                ServiceBaseId / "ComputerScheduler",
                typeof(ComputerScheduler),
                initializer => new ComputerScheduler(
                    initializer.GetSingle<IMonitor>(ServiceBaseId / "Monitor"),
                    initializer.GetSingle<Configuration>(ServiceBaseId / "Configuration")
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "PlayerTransport",
                typeof(SmapiPlayerTransport),
                initializer => new SmapiPlayerTransport(
                    initializer.GetSingle<IModHelper>(ServiceBaseId / "ModHelper"),
                    ModManifest
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "HostChannel",
                typeof(Multiplayer.Domain.HostChannel),
                initializer => {
                    var monitor = initializer.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
                    return new Multiplayer.Domain.HostChannel(
                        initializer.GetSingle<SmapiPlayerTransport>(ServiceBaseId / "PlayerTransport"),
                        message => monitor.Log(message)
                    );
                }
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ScreenCast",
                typeof(Multiplayer.Domain.ScreenCast),
                initializer => {
                    var monitor = initializer.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
                    var configuration = initializer.GetSingle<Configuration>(ServiceBaseId / "Configuration");
                    return new Multiplayer.Domain.ScreenCast(
                        initializer.GetSingle<Multiplayer.Domain.HostChannel>(ServiceBaseId / "HostChannel"),
                        configuration.Multiplayer.CastTicksPerFrame,
                        configuration.Multiplayer.MaxViewersPerComputer,
                        message => monitor.Log(message)
                    );
                }
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ClientChannels",
                typeof(StardewModdingAPI.Utilities.PerScreen<Multiplayer.Domain.ClientChannel>),
                initializer => {
                    var monitor = initializer.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
                    var configuration = initializer.GetSingle<Configuration>(ServiceBaseId / "Configuration");
                    var transport = initializer.GetSingle<SmapiPlayerTransport>(ServiceBaseId / "PlayerTransport");
                    return new StardewModdingAPI.Utilities.PerScreen<Multiplayer.Domain.ClientChannel>(
                        () => new Multiplayer.Domain.ClientChannel(
                            transport,
                            configuration.Multiplayer.CallTimeoutTicks,
                            message => monitor.Log(message)
                        )
                    );
                }
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "FrameTap",
                typeof(IFrameTap),
                initializer => initializer.GetSingle<Multiplayer.Domain.ScreenCast>(ServiceBaseId / "ScreenCast")
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "SchedulerPulseDispatcher",
                typeof(IEventHandler),
                initializer => new SchedulerPulseDispatcher(
                    initializer.GetSingle<ComputerScheduler>(ServiceBaseId / "ComputerScheduler")
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "NetworkRegistry",
                typeof(NetworkRegistry),
                initializer => new NetworkRegistry(
                    initializer.GetSingle<IMonitor>(ServiceBaseId / "Monitor"),
                    initializer.GetSingle<Configuration>(ServiceBaseId / "Configuration"),
                    initializer.Lookup<IRouterPort>()
                )
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
                    initializer.GetSingle<Random>(),
                    initializer.GetSingle<IRedundantLoader>(ServiceBaseId / "RedundantLoader"),
                    initializer.GetSingle<IRedundantLoader>(ServiceBaseId / "AssetsLoader"),
                    initializer.GetSingle<IRedundantLoader>(ServiceBaseId / "DataLoader"),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<IRouterPort>(),
                    initializer.GetSingle<ComputerScheduler>(ServiceBaseId / "ComputerScheduler"),
                    initializer.GetSingle<IFrameTap>(ServiceBaseId / "FrameTap")
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
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<INetworkEndpoint>(),
                    initializer.Lookup<IRouterPort>(),
                    PeripheralTier.Basic
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
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "MachineWorld",
                typeof(IMachineWorld),
                _ => new StardewMachineWorld()
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "WeatherWorld",
                typeof(IWeatherWorld),
                _ => new StardewWeatherWorld()
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "SensorWorld",
                typeof(ISensorWorld),
                _ => new StardewSensorWorld()
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "MailerWorld",
                typeof(IMailerWorld),
                initializer => new StardewMailerWorld(initializer.GetSingle<IModHelper>())
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ShippingWorld",
                typeof(IShippingWorld),
                _ => new StardewShippingWorld()
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "SpeakerWorld",
                typeof(ISpeakerWorld),
                _ => new StardewSpeakerWorld()
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "MailPatcher",
                typeof(IPatcherService),
                initializer => new MailPatcherService(
                    initializer.Lookup<ILetterSource>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "MailerFactory",
                typeof(IPeripheralFactory),
                initializer => new MailerStatefulDataContextEntryFactory(
                    ServiceBaseId / "MailerFactory",
                    MailerBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<IRouterPort>(),
                    initializer.GetSingle<IMailerWorld>(ServiceBaseId / "MailerWorld")
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "ShippingControllerFactory",
                typeof(IPeripheralFactory),
                initializer => new ShippingControllerStatefulDataContextEntryFactory(
                    ServiceBaseId / "ShippingControllerFactory",
                    ShippingControllerBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<IRouterPort>(),
                    initializer.GetSingle<IShippingWorld>(ServiceBaseId / "ShippingWorld"),
                    PeripheralTier.Basic
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "SpeakerFactory",
                typeof(IPeripheralFactory),
                initializer => new SpeakerStatefulDataContextEntryFactory(
                    ServiceBaseId / "SpeakerFactory",
                    SpeakerBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<IRouterPort>(),
                    initializer.GetSingle<ISpeakerWorld>(ServiceBaseId / "SpeakerWorld")
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "AdvancedMachineControllerFactory",
                typeof(IPeripheralFactory),
                initializer => new MachineControllerStatefulDataContextEntryFactory(
                    ServiceBaseId / "AdvancedMachineControllerFactory",
                    AdvancedMachineControllerBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<IRouterPort>(),
                    initializer.GetSingle<IMachineWorld>(ServiceBaseId / "MachineWorld"),
                    PeripheralTier.Advanced
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "AdvancedShippingControllerFactory",
                typeof(IPeripheralFactory),
                initializer => new ShippingControllerStatefulDataContextEntryFactory(
                    ServiceBaseId / "AdvancedShippingControllerFactory",
                    AdvancedShippingControllerBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<IRouterPort>(),
                    initializer.GetSingle<IShippingWorld>(ServiceBaseId / "ShippingWorld"),
                    PeripheralTier.Advanced
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "AdvancedPlayerSensorFactory",
                typeof(IPeripheralFactory),
                initializer => new PlayerSensorStatefulDataContextEntryFactory(
                    ServiceBaseId / "AdvancedPlayerSensorFactory",
                    AdvancedPlayerSensorBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<IRouterPort>(),
                    initializer.GetSingle<ISensorWorld>(ServiceBaseId / "SensorWorld"),
                    PeripheralTier.Advanced
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "AdvancedRouterFactory",
                typeof(IStatefulDataContextEntryFactory),
                initializer => new RouterStatefulDataContextEntryFactory(
                    ServiceBaseId / "AdvancedRouterFactory",
                    AdvancedRouterBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<INetworkEndpoint>(),
                    initializer.Lookup<IRouterPort>(),
                    PeripheralTier.Advanced
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "WeatherStationFactory",
                typeof(IPeripheralFactory),
                initializer => new WeatherStationStatefulDataContextEntryFactory(
                    ServiceBaseId / "WeatherStationFactory",
                    WeatherStationBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<IRouterPort>(),
                    initializer.GetSingle<IWeatherWorld>(ServiceBaseId / "WeatherWorld")
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "PlayerSensorFactory",
                typeof(IPeripheralFactory),
                initializer => new PlayerSensorStatefulDataContextEntryFactory(
                    ServiceBaseId / "PlayerSensorFactory",
                    PlayerSensorBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<IRouterPort>(),
                    initializer.GetSingle<ISensorWorld>(ServiceBaseId / "SensorWorld"),
                    PeripheralTier.Basic
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "MachineControllerFactory",
                typeof(IPeripheralFactory),
                initializer => new MachineControllerStatefulDataContextEntryFactory(
                    ServiceBaseId / "MachineControllerFactory",
                    MachineControllerBigCraftableId,
                    initializer.GetSingle<IMonitor>(),
                    initializer.GetSingle<Configuration>(),
                    initializer.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry"),
                    initializer.Lookup<IRouterPort>(),
                    initializer.GetSingle<IMachineWorld>(ServiceBaseId / "MachineWorld"),
                    PeripheralTier.Basic
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "PeripheralTickDispatcher",
                typeof(IEventHandler),
                initializer => new PeripheralTickDispatcher(
                    initializer.Lookup<IPeripheralPort>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "PeripheralStartDispatcher",
                typeof(IEventHandler),
                initializer => new PeripheralStartDispatcher(
                    initializer.Lookup<IPeripheralPort>()
                )
            ),
            new IContextEntry.ServiceContextEntry(
                ServiceBaseId / "PeripheralStopDispatcher",
                typeof(IEventHandler),
                initializer => new PeripheralStopDispatcher(
                    initializer.Lookup<IPeripheralPort>()
                )
            )
        );

        var eventBus = _context.GetSingle<IEventBus>(ServiceBaseId / "EventBus");

        static bool IsMainScreen() {
            return StardewModdingAPI.Context.ScreenId == 0;
        }

        helper.Events.Content.AssetRequested += (_, e) => eventBus.Publish(new AssetRequestedEvent(e));

        helper.Events.GameLoop.GameLaunched += (_, e) => {
            if (IsMainScreen()) eventBus.Publish(new GameLaunchedEvent(e));
        };
        helper.Events.GameLoop.UpdateTicked += (_, e) => {
            if (IsMainScreen()) eventBus.Publish(new UpdateTickedEvent(e));
        };
        helper.Events.GameLoop.ReturnedToTitle += (_, e) => {
            if (!IsMainScreen()) return;
            _context.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry").Clear();
            eventBus.Publish(new ReturnedToTitleEvent(e));
        };

        helper.Events.Input.ButtonPressed += (_, e) => {
            if (IsMainScreen()) eventBus.Publish(new ButtonPressedEvent(e));
        };
        helper.Events.Input.ButtonReleased += (_, e) => {
            if (IsMainScreen()) eventBus.Publish(new ButtonReleasedEvent(e));
        };

        helper.Events.World.ObjectListChanged += (_, e) => {
            if (IsMainScreen()) HandleObjectListChanged(e);
        };
        helper.Events.GameLoop.SaveCreating += (_, _) => {
            if (IsMainScreen()) HandleSave();
        };
        helper.Events.GameLoop.Saving += (_, _) => {
            if (IsMainScreen()) HandleSave();
        };
        helper.Events.GameLoop.SaveLoaded += (_, e) => {
            if (IsMainScreen()) HandleLoad(e);
        };

        RegisterMultiplayer(helper);
        RegisterDevAutoload(helper);
    }

    private static Multiplayer.Domain.ClientChannel CurrentClientChannel =>
        _context.GetSingle<StardewModdingAPI.Utilities.PerScreen<Multiplayer.Domain.ClientChannel>>(
            ServiceBaseId / "ClientChannels"
        ).Value;

    private static void RegisterMultiplayer(IModHelper helper) {
        var transport = _context.GetSingle<SmapiPlayerTransport>(ServiceBaseId / "PlayerTransport");
        var hostChannel = _context.GetSingle<Multiplayer.Domain.HostChannel>(ServiceBaseId / "HostChannel");
        var screenCast = _context.GetSingle<Multiplayer.Domain.ScreenCast>(ServiceBaseId / "ScreenCast");
        var clientChannels = _context.GetSingle<StardewModdingAPI.Utilities.PerScreen<Multiplayer.Domain.ClientChannel>>(
            ServiceBaseId / "ClientChannels"
        );

        RegisterChannelHandlers();

        helper.Events.GameLoop.UpdateTicked += (_, _) => {
            transport.NoteLocalPlayer();
            if (transport.IsHost) {
                hostChannel.Tick();
                screenCast.Tick();
                TryStampPendingComputers();
            }
            else {
                clientChannels.Value.Tick();
            }
        };

        helper.Events.Multiplayer.PeerDisconnected += (_, e) => {
            if (StardewModdingAPI.Context.ScreenId == 0) {
                screenCast.DropPlayer(e.Peer.PlayerID);
            }
        };
    }

    private static void RegisterChannelHandlers() {
        var configuration = _context.GetSingle<Configuration>(ServiceBaseId / "Configuration");
        var hostChannel = _context.GetSingle<Multiplayer.Domain.HostChannel>(ServiceBaseId / "HostChannel");
        var screenCast = _context.GetSingle<Multiplayer.Domain.ScreenCast>(ServiceBaseId / "ScreenCast");

        hostChannel.Register("openScreen", (playerId, request) => {
            var open = (Multiplayer.Domain.Wire.OpenScreenRequest) request;
            var obj = ChannelObjectAt(open.X, open.Y, open.Location);
            var heldModData = obj.HeldObjectModData();
            if (heldModData is null || !heldModData.TryGetValue("ComputerId", out var computerId)) {
                throw new Multiplayer.Domain.Wire.ChannelRequestException("no computer at that tile");
            }
            return screenCast.Subscribe(computerId, playerId, configuration.Render.CanvasWidth, configuration.Render.CanvasHeight);
        });

        hostChannel.Register("closeScreen", (playerId, request) => {
            screenCast.Unsubscribe(((Multiplayer.Domain.Wire.CloseScreenRequest) request).ComputerId, playerId);
            return null;
        });

        hostChannel.Register("screenInput", (_, request) => {
            var input = (Computers.Multiplayer.Domain.Wire.ScreenInputRequest) request;
            if (!_context.TryGetSingle<IComputerPort>(input.ComputerId.AsId(), out var computer)) {
                return null;
            }
            switch (input.Kind) {
                case "key":
                    computer.Fire(new KeyPressedEvent(computer.Id, (Microsoft.Xna.Framework.Input.Keys) input.A));
                    break;
                case "leftClick":
                    computer.Fire(new MouseLeftClickedEvent(computer.Id, input.A, input.B));
                    break;
                case "rightClick":
                    computer.Fire(new MouseRightClickedEvent(computer.Id, input.A, input.B));
                    break;
                case "wheel":
                    computer.Fire(new MouseWheelEvent(computer.Id, input.A));
                    break;
            }
            return null;
        });

        hostChannel.Register("initializeComputer", (_, request) => {
            var init = (Multiplayer.Domain.Wire.InitializeComputerRequest) request;
            PendingComputerStamps.Add(new PendingStamp(init.X, init.Y, init.Location) { TicksLeft = 600 });
            TryStampPendingComputers();
            return null;
        });
    }

    private static Object ChannelObjectAt(int x, int y, string locationName) {
        var location = Game1.getLocationFromName(locationName)
            ?? throw new Multiplayer.Domain.Wire.ChannelRequestException($"location '{locationName}' not found");
        return !location.objects.TryGetValue(new Vector2(x, y), out var obj)
            ? throw new Multiplayer.Domain.Wire.ChannelRequestException("no object at that tile") 
            : obj;
    }

    private sealed record PendingStamp(int X, int Y, string Location) {
        public int TicksLeft { get; set; }
    }

    private static readonly List<PendingStamp> PendingComputerStamps = new();

    private static void TryStampPendingComputers() {
        for (var index = PendingComputerStamps.Count - 1; index >= 0; index--) {
            var pending = PendingComputerStamps[index];
            if (--pending.TicksLeft <= 0) {
                PendingComputerStamps.RemoveAt(index);
                continue;
            }

            var location = Game1.getLocationFromName(pending.Location);
            if (location is null || !location.objects.TryGetValue(new Vector2(pending.X, pending.Y), out var obj)) {
                continue;
            }

            var held = obj.heldObject.Value;
            if (held is null) {
                continue;
            }

            if (!held.modData.ContainsKey("ComputerId")) {
                var computer = _context.ProduceSingle<ComputerStatefulDataContextEntry>(ServiceBaseId / "ComputerFactory");
                held.modData["ComputerId"] = computer.Id;
                computer.Start();
                _context.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry").Register(new Placement(
                    computer.Id,
                    NodeRole.Endpoint,
                    location.NameOrUniqueName,
                    pending.X,
                    pending.Y
                ));
            }
            PendingComputerStamps.RemoveAt(index);
        }
    }

    private void RegisterDevAutoload(IModHelper helper) {
        var devLoadSave = Environment.GetEnvironmentVariable("COMPUTERS_DEV_LOAD_SAVE");
        if (string.IsNullOrEmpty(devLoadSave)) {
            return;
        }

        StardewValley.Menus.TitleMenu.SkipSplashScreens = true;

        var triggered = false;
        helper.Events.GameLoop.UpdateTicked += (_, _) => {
            if (triggered || Game1.activeClickableMenu is not StardewValley.Menus.TitleMenu) {
                return;
            }

            triggered = true;

            var savesPath = Constants.SavesPath;
            var saveFolder = Directory.Exists(savesPath)
                ? Directory.EnumerateDirectories(savesPath)
                    .Select(Path.GetFileName)
                    .WhereNotNull()
                    .FirstOrDefault(name => name == devLoadSave || name.StartsWith($"{devLoadSave}_"))
                : null;

            if (saveFolder is null) {
                Monitor.Log($"Dev autoload: no save folder matching '{devLoadSave}' found in {savesPath}.", LogLevel.Warn);
                return;
            }

            Monitor.Log($"Dev autoload: loading save '{saveFolder}'.", LogLevel.Info);
            SaveGame.Load(saveFolder);
            Game1.exitActiveMenu();
        };
    }

    public static bool ComputerMachineInteractMethod(
        Object machine, 
        GameLocation location,
        Farmer player
    ) {
        if (!Game1.IsMasterGame || StardewModdingAPI.Context.ScreenId != 0) {
            OpenRemoteScreen(machine, location);
            return true;
        }

        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log($"Interacted with computer machine. Machine: {machine}, Location: {location}, Player: {player}, Held Object: {machine.heldObject}");

        var modData = machine.HeldObjectModData();
        if (modData == null) {
            monitor.Log("Computer does not have a held object - cannot infer script.");
            Game1.showGlobalMessage("Computer has no disk inserted.");
            return true;
        }

        if (!modData.ContainsKey("ComputerId")) {
            monitor.Log("Computer does not have a script id - cannot infer script.");
            Game1.showGlobalMessage("The inserted disk is not initialized.");
            return true;
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
        Farmer player,
        out int? overrideMinutesUntilReady
    ) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log($"Preparing output from computer machine. Machine: {machine}, InputItem: {inputItem}, Probe: {probe}, OutputData: {outputData}");
        
        overrideMinutesUntilReady = null;
        var outputItem = inputItem.getOne();
        
        if (probe) {
            return outputItem;
        }

        if (!Game1.IsMasterGame || StardewModdingAPI.Context.ScreenId != 0) {
            CurrentClientChannel.Send("initializeComputer", new {
                x = (int) machine.TileLocation.X,
                y = (int) machine.TileLocation.Y,
                location = machine.Location.NameOrUniqueName
            });
            return outputItem;
        }

        IComputerPort computer;
        if (outputItem.modData.ContainsKey("ComputerId") && _context.TryGetSingle<IComputerPort>(outputItem.modData["ComputerId"].AsId(), out var existingComputer)) {
            monitor.Log("ComputerId already exists.");
            computer = existingComputer;
        }
        else {
            computer = _context.ProduceSingle<ComputerStatefulDataContextEntry>(ServiceBaseId / "ComputerFactory");
            monitor.Log($"Setting ComputerId to {computer.Id}");
            outputItem.modData["ComputerId"] = computer.Id;
        }
        
        computer.Start();

        var registry = _context.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry");
        registry.Register(new Placement(
            computer.Id,
            NodeRole.Endpoint,
            machine.Location.NameOrUniqueName,
            (int) machine.TileLocation.X,
            (int) machine.TileLocation.Y
        ));

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

        Game1.showGlobalMessage(_context.TryGetSingle<IRouterPort>(routerId, out var routerPort)
            ? $"Router Id: {routerId.Last}, Channel: {routerPort.Channel?.ToString() ?? "none"}"
            : $"Router Id: {routerId.Last}");
        return true;
    }

    public static bool PeripheralMachineInteractMethod(
        Object machine,
        GameLocation location,
        Farmer player
    ) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log($"Interacted with machine controller. Machine: {machine}, Location: {location}, Player: {player}");

        var modData = machine.modData;
        if (modData == null || !modData.ContainsKey("PeripheralId")) {
            monitor.Log("Peripheral does not have an id - cannot show it.");
            return true;
        }

        var peripheralId = modData["PeripheralId"].AsId();
        Game1.showGlobalMessage($"Peripheral Id: {peripheralId.Last}");
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
        var skipped = _context.Restore(deserializedData);
        foreach (var skippedId in skipped) {
            monitor.Log($"Skipped restoring '{skippedId}': its factory is gone. The stale state drops from the next save.", LogLevel.Warn);
        }

        // Rebuild network placements from the loaded world.
        var registry = _context.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry");
        var placements = new List<Placement>();
        Utility.ForEachLocation(location => {
            foreach (var (tile, obj) in location.objects.Pairs) {
                if (obj.modData.ContainsKey("RouterId")) {
                    placements.Add(new Placement(
                        obj.modData["RouterId"].AsId(),
                        NodeRole.Router,
                        location.NameOrUniqueName,
                        (int) tile.X,
                        (int) tile.Y
                    ));
                }

                if (obj.modData.ContainsKey("PeripheralId")) {
                    placements.Add(new Placement(
                        obj.modData["PeripheralId"].AsId(),
                        NodeRole.Endpoint,
                        location.NameOrUniqueName,
                        (int) tile.X,
                        (int) tile.Y
                    ));
                }

                var heldModData = obj.HeldObjectModData();
                if (heldModData is not null && heldModData.ContainsKey("ComputerId")) {
                    placements.Add(new Placement(
                        heldModData["ComputerId"].AsId(),
                        NodeRole.Endpoint,
                        location.NameOrUniqueName,
                        (int) tile.X,
                        (int) tile.Y
                    ));
                }
            }
            return true;
        });
        registry.ReplaceAll(placements);

        eventBus.Publish(new SaveLoadedEvent(args));
    }

    private static void HandleObjectListChanged(ObjectListChangedEventArgs args) {
        if (!Game1.IsMasterGame) {
            return;
        }

        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        var registry = _context.GetSingle<NetworkRegistry>(ServiceBaseId / "NetworkRegistry");
        var locationName = args.Location.NameOrUniqueName;

        foreach (var (position, obj) in args.Added) {
            monitor.Log($"Added object at {position}: {obj}");

            if (obj.ItemId == RouterBigCraftableId || obj.ItemId == AdvancedRouterBigCraftableId) {
                HandleRouterAdded(obj, position, obj.ItemId == AdvancedRouterBigCraftableId
                    ? ServiceBaseId / "AdvancedRouterFactory"
                    : ServiceBaseId / "RouterFactory");
                registry.Register(new Placement(
                    obj.modData["RouterId"].AsId(),
                    NodeRole.Router,
                    locationName,
                    (int) position.X,
                    (int) position.Y
                ));
            }

            if (PeripheralFactoryFor(obj.ItemId) is { } peripheralFactory) {
                HandlePeripheralAdded(obj, position, peripheralFactory);
                registry.Register(new Placement(
                    obj.modData["PeripheralId"].AsId(),
                    NodeRole.Endpoint,
                    locationName,
                    (int) position.X,
                    (int) position.Y
                ));
            }

            var heldModData = obj.HeldObjectModData();
            if (heldModData is not null && heldModData.ContainsKey("ComputerId")) {
                registry.Register(new Placement(
                    heldModData["ComputerId"].AsId(),
                    NodeRole.Endpoint,
                    locationName,
                    (int) position.X,
                    (int) position.Y
                ));
            }
        }

        foreach(var (position, obj) in args.Removed) {
            monitor.Log($"Removed object at {position}: {obj}");

            var objectData = obj.modData;
            var heldObjectModData = obj.HeldObjectModData();

            if (heldObjectModData is not null && heldObjectModData.ContainsKey("ComputerId")) {
                var computerId = heldObjectModData["ComputerId"].AsId();
                HandleComputerRemoved(computerId, obj, position);
                registry.Unregister(computerId);
            }

            if (objectData is not null && objectData.ContainsKey("RouterId")) {
                var routerId = objectData["RouterId"].AsId();
                HandleRouterRemoved(routerId, obj, position);
                registry.Unregister(routerId);
            }

            if (objectData is not null && objectData.ContainsKey("PeripheralId")) {
                var peripheralId = objectData["PeripheralId"].AsId();
                HandlePeripheralRemoved(peripheralId);
                registry.Unregister(peripheralId);
            }
        }

        // Any nearby object change may alter machine groups.
        _context.Get<IPeripheralPort>().ForEach(peripheral => peripheral.Value.NotifyWorldChanged());
    }

    private static IPeripheralFactory? PeripheralFactoryFor(string itemId) {
        return _context.Get<IPeripheralFactory>()
            .Select(entry => entry.Value)
            .SingleOrDefault(factory => factory.ItemId == itemId);
    }

    private static void HandlePeripheralAdded(Object obj, Vector2 position, IPeripheralFactory factory) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log("Peripheral added.");

        IPeripheralPort peripheral;
        if (obj.modData.ContainsKey("PeripheralId") && _context.TryGetSingle<IPeripheralPort>(obj.modData["PeripheralId"].AsId(), out var existingPeripheral)) {
            monitor.Log("PeripheralId already exists.");
            peripheral = existingPeripheral;
        } else {
            peripheral = _context.ProduceSingle<IPeripheralPort>(factory);
            monitor.Log($"Setting PeripheralId to {peripheral.Id}");
            obj.modData["PeripheralId"] = peripheral.Id;
        }

        peripheral.Start();
    }

    private static void HandlePeripheralRemoved(Id peripheralId) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log($"Peripheral with id {peripheralId} was removed.");

        if (!_context.TryGetSingle<IPeripheralPort>(peripheralId, out var peripheral)) {
            monitor.Log($"Peripheral {peripheralId} has no entry, nothing to stop.", LogLevel.Warn);
            return;
        }
        peripheral.Stop();
    }

    private static void HandleRouterAdded(Object obj, Vector2 position, Id routerFactoryId) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log("Router added.");

        IRouterPort router;
        if (obj.modData.ContainsKey("RouterId") && _context.TryGetSingle<IRouterPort>(obj.modData["RouterId"].AsId(), out var existingRouter)) {
            monitor.Log("RouterId already exists.");
            router = existingRouter;
        } else {
            router = _context.ProduceSingle<RouterStatefulDataContextEntry>(routerFactoryId);
            monitor.Log($"Setting RouterId to {router.Id}");
            obj.modData["RouterId"] = router.Id;
        }

        router.Start();
    }
    
    private static void HandleRouterRemoved(Id routerId, Object obj, Vector2 position) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log($"Router with id {routerId} was removed.");
        
        if (!_context.TryGetSingle<IRouterPort>(routerId, out var router)) {
            monitor.Log($"Router {routerId} has no entry, nothing to stop.", LogLevel.Warn);
            return;
        }
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

        // Anyone still viewing this screen remotely gets told it closed.
        _context.GetSingle<Multiplayer.Domain.ScreenCast>(ServiceBaseId / "ScreenCast").DropComputer(computerId);

        // Stop computer
        if (_context.TryGetSingle<IComputerPort>(computerId, out var computerState)) {
            computerState.Fire(new StopComputerEvent(computerState.Id));
        } else {
            monitor.Log($"Computer {computerId} has no entry, nothing to stop.", LogLevel.Warn);
        }
            
        // Make a disk with the script
        Game1.createItemDebris(
            heldObject,
            position * Game1.tileSize,
            Game1.random.Next(4),
            Game1.player.currentLocation
        );
    }
    
    private static void OpenRemoteScreen(Object machine, GameLocation location) {
        var monitor = _context.GetSingle<IMonitor>(ServiceBaseId / "Monitor");
        monitor.Log($"Remote screen open requested on screen {StardewModdingAPI.Context.ScreenId}, master game {Game1.IsMasterGame}.");
        var channel = CurrentClientChannel;
        var configuration = _context.GetSingle<Configuration>(ServiceBaseId / "Configuration");
        var assetLoader = _context.GetSingle<IRedundantLoader>(ServiceBaseId / "AssetsLoader");
        channel.Call(
            "openScreen",
            new {
                x = (int) machine.TileLocation.X,
                y = (int) machine.TileLocation.Y,
                location = location.NameOrUniqueName
            },
            data => {
                monitor.Log($"Remote screen opening on screen {StardewModdingAPI.Context.ScreenId}.");
                RemoteScreen.Open(channel, data!["computerId"]!.ToObject<string>()!, configuration, assetLoader);
            },
            error => {
                monitor.Log($"Remote screen open failed. {error}");
                Game1.showGlobalMessage(error);
            }
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
