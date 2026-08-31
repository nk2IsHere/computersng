using System.Text;
using Computers;
using Computers.Computer;
using Computers.Computer.Domain;
using Computers.Core;
using Computers.E2e.Wire;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace Computers.E2e;

// Implements every control command against the running game. Each command becomes a
// pending operation polled on the game thread, so multi tick work such as waiting for
// placement handlers is a matter of returning not finished yet. Errors are thrown and
// the control service turns them into failure replies.
public class E2eOperations {
    // The vanilla new game flow reaches the playing mode before it runs the initial
    // save creation, whose SaveLoaded event makes the mod restore every entity. The
    // world only counts as ready once a SaveLoaded has settled for a grace period, so
    // tests cannot race that restore.
    private const int WorldReadyGraceTicks = 60;

    private volatile bool _saved;
    private long _tick;
    private long? _saveLoadedAtTick;

    // Short names the place command accepts, mapped to the main mod's item identities.
    private static readonly IReadOnlyDictionary<string, string> ItemsByName = new Dictionary<string, string> {
        ["computer"] = ModEntry.ComputerBigCraftableId,
        ["router"] = ModEntry.RouterBigCraftableId,
        ["advancedRouter"] = ModEntry.AdvancedRouterBigCraftableId,
        ["machineController"] = ModEntry.MachineControllerBigCraftableId,
        ["advancedMachineController"] = ModEntry.AdvancedMachineControllerBigCraftableId,
        ["weatherStation"] = ModEntry.WeatherStationBigCraftableId,
        ["playerSensor"] = ModEntry.PlayerSensorBigCraftableId,
        ["advancedPlayerSensor"] = ModEntry.AdvancedPlayerSensorBigCraftableId,
        ["mailer"] = ModEntry.MailerBigCraftableId,
        ["shippingController"] = ModEntry.ShippingControllerBigCraftableId,
        ["advancedShippingController"] = ModEntry.AdvancedShippingControllerBigCraftableId,
        ["speaker"] = ModEntry.SpeakerBigCraftableId
    };

    // Vanilla machines the place command accepts. They carry no mod identity, so
    // placement completes as soon as the object exists.
    private static readonly IReadOnlyDictionary<string, string> VanillaItemsByName = new Dictionary<string, string> {
        ["furnace"] = "13",
        // A computer craftable with no disk inserted, for scenarios about the machine
        // shell itself. No mod identity is expected, so it completes like vanilla items.
        ["computerShell"] = ModEntry.ComputerBigCraftableId
    };

    private sealed class Operation : IPendingOperation {
        private readonly Func<(bool Finished, object? Data)> _poll;

        public Operation(Func<(bool Finished, object? Data)> poll) {
            _poll = poll;
        }

        public bool TryComplete(out object? data, out string? error) {
            error = null;
            var (finished, result) = _poll();
            data = result;
            return finished;
        }
    }

    private static Operation Immediate(Func<object?> run) {
        return new Operation(() => (true, run()));
    }

    public void NotifySaved() {
        _saved = true;
    }

    public void NotifyTick() {
        _tick++;
    }

    public void NotifySaveLoaded() {
        _saveLoadedAtTick = _tick;
    }

    public IPendingOperation Create(E2eRequest request) {
        return request switch {
            StatusRequest => Immediate(Status),
            PlaceRequest place => Place(place),
            PlaceChestRequest chest => Immediate(() => PlaceChest(chest)),
            RemoveRequest remove => Immediate(() => Remove(remove)),
            WriteDiskRequest write => Immediate(() => WriteDisk(write)),
            ReadDiskRequest read => Immediate(() => ReadDisk(read)),
            RestartComputerRequest restart => RestartComputer(restart),
            InteractRequest interact => Immediate(() => Interact(interact)),
            QueryObjectRequest query => Immediate(() => QueryObject(query)),
            WaitTicksRequest wait => WaitTicks(wait.Count),
            QueryShippingBinRequest => Immediate(QueryShippingBin),
            QueryFarmhouseRequest => Immediate(QueryFarmhouse),
            QueryActiveMenuRequest => Immediate(() => new ActiveMenuResult(Game1.activeClickableMenu?.GetType().Name)),
            MenuKeyRequest menuKey => Immediate(() => MenuKey(menuKey)),
            MenuClickRequest menuClick => Immediate(() => MenuClick(menuClick)),
            StartSplitScreenRequest => StartSplitScreen(),
            InsertDiskRequest insertDisk => Immediate(() => InsertDisk(insertDisk)),
            SaveGameRequest => SaveGame(),
            QuitRequest => Immediate(Quit),
            _ => throw new E2eRequestException("command is not implemented")
        };
    }

    private object Status() {
        var gameMode = Game1.gameMode switch {
            Game1.titleScreenGameMode => "title",
            Game1.playingGameMode => "playing",
            Game1.loadingMode => "loading",
            _ => Game1.gameMode.ToString()
        };
        var playerCount = Game1.hasLoadedGame ? Game1.getOnlineFarmers().Count : 0;
        var worldReady = Game1.hasLoadedGame
            && Game1.gameMode == Game1.playingGameMode
            && _saveLoadedAtTick is not null
            && _tick - _saveLoadedAtTick >= WorldReadyGraceTicks;
        return new StatusResult(gameMode, Game1.hasLoadedGame, playerCount, worldReady);
    }

    private static IPendingOperation Place(PlaceRequest request) {
        var isVanilla = VanillaItemsByName.TryGetValue(request.Item, out var itemId);
        if (!isVanilla && !ItemsByName.TryGetValue(request.Item, out itemId)) {
            throw new E2eRequestException($"unknown item '{request.Item}'");
        }

        Object? placed = null;
        return new Operation(() => {
            if (placed is null) {
                var location = ResolveLocation(request.Location);
                var tile = new Vector2(request.X, request.Y);
                location.objects.Remove(tile);
                location.terrainFeatures.Remove(tile);

                placed = ItemRegistry.Create<Object>($"(BC){itemId}");
                placed.TileLocation = tile;
                location.objects.Add(tile, placed);

                if (request.Item == "computer") {
                    var disk = ItemRegistry.Create($"(O){ModEntry.DiskItemId}");
                    placed.performObjectDropInAction(disk, false, Game1.player);
                }
                return (false, null);
            }

            if (isVanilla) {
                return (true, new PlaceResult(null));
            }

            var id = IdentityOf(placed);
            return id is null ? (false, null) : (true, new PlaceResult(id));
        });
    }

    private static object? PlaceChest(PlaceChestRequest request) {
        var location = ResolveLocation(request.Location);
        var tile = new Vector2(request.X, request.Y);
        location.objects.Remove(tile);
        location.terrainFeatures.Remove(tile);

        var chest = new Chest(true) { TileLocation = tile };
        foreach (var item in request.Items) {
            chest.Items.Add(ItemRegistry.Create(item.ItemId, item.Count));
        }
        location.objects.Add(tile, chest);
        return null;
    }

    private static object? Remove(RemoveRequest request) {
        var location = ResolveLocation(request.Location);
        var tile = new Vector2(request.X, request.Y);
        if (!location.objects.Remove(tile)) {
            throw new E2eRequestException("no object at that tile");
        }
        return null;
    }

    private static object? WriteDisk(WriteDiskRequest request) {
        var storage = ComputerAt(request.X, request.Y, request.Location).PersistentStorage;
        if (storage.Exists(request.Path)) {
            storage.Delete(request.Path);
        }
        var response = storage.Write(request.Path, Encoding.UTF8.GetBytes(request.Content));
        if (response.Type != StorageResponseType.Success) {
            throw new E2eRequestException($"write failed with {response.Error}");
        }
        return null;
    }

    private static object ReadDisk(ReadDiskRequest request) {
        var storage = ComputerAt(request.X, request.Y, request.Location).PersistentStorage;
        var response = storage.Read(request.Path);
        if (response.Type != StorageResponseType.Success || response.Data is null) {
            throw new E2eRequestException("file not found");
        }
        return new ReadDiskResult(Encoding.UTF8.GetString(response.Data.Data));
    }

    // Stop and start are separated by a grace period so the in flight slice can drain
    // after the cancel. Start makes the next slice boot, and boot reloads the engine on
    // the worker itself, so the game thread never tears an engine down mid slice.
    private const int RestartGraceTicks = 15;

    private static IPendingOperation RestartComputer(RestartComputerRequest request) {
        var stopped = false;
        var remaining = RestartGraceTicks;
        return new Operation(() => {
            if (!stopped) {
                stopped = true;
                ComputerAt(request.X, request.Y, request.Location).Stop();
                return (false, null);
            }
            if (--remaining > 0) {
                return (false, null);
            }
            ComputerAt(request.X, request.Y, request.Location).Start();
            return (true, null);
        });
    }

    private static object? Interact(InteractRequest request) {
        var obj = ObjectAt(request.X, request.Y, request.Location);
        obj.checkForAction(Game1.player);
        return null;
    }

    private static object QueryObject(QueryObjectRequest request) {
        var obj = ObjectAt(request.X, request.Y, request.Location);
        var chestItems = obj is Chest chest
            ? chest.Items.Where(item => item is not null).Select(item => new ShippedItem(item.ItemId, item.Name, item.Stack)).ToList()
            : null;
        return new ObjectResult(obj.ItemId, IdentityOf(obj), chestItems);
    }

    private static IPendingOperation WaitTicks(int count) {
        var remaining = count;
        return new Operation(() => (--remaining <= 0, null));
    }

    private static object? MenuKey(MenuKeyRequest request) {
        if (Game1.activeClickableMenu is null) {
            throw new E2eRequestException("no menu is open on this screen");
        }
        Game1.activeClickableMenu.receiveKeyPress((Microsoft.Xna.Framework.Input.Keys) request.Key);
        return null;
    }

    private static object? MenuClick(MenuClickRequest request) {
        if (Game1.activeClickableMenu is null) {
            throw new E2eRequestException("no menu is open on this screen");
        }
        if (request.Button == "right") {
            Game1.activeClickableMenu.receiveRightClick(request.X, request.Y);
        } else {
            Game1.activeClickableMenu.receiveLeftClick(request.X, request.Y);
        }
        return null;
    }

    private static IPendingOperation StartSplitScreen() {
        var started = false;
        return new Operation(() => {
            if (!started) {
                started = true;
                StardewValley.GameRunner.instance.AddGameInstance(Microsoft.Xna.Framework.PlayerIndex.Two);
                return (false, null);
            }
            return (StardewValley.GameRunner.instance.gameInstances.Count >= 2, null);
        });
    }

    private static object? InsertDisk(InsertDiskRequest request) {
        var obj = ObjectAt(request.X, request.Y, request.Location);
        var disk = ItemRegistry.Create($"(O){ModEntry.DiskItemId}");
        obj.performObjectDropInAction(disk, false, Game1.player);
        return null;
    }

    private static object QueryFarmhouse() {
        var entry = Game1.getFarm().GetMainFarmHouseEntry();
        return new FarmhouseResult(entry.X, entry.Y);
    }

    private static object QueryShippingBin() {
        var items = Game1.getFarm()
            .getShippingBin(Game1.player)
            .Select(item => new ShippedItem(item.ItemId, item.Name, item.Stack))
            .ToList();
        return new ShippingBinResult(items);
    }

    private IPendingOperation SaveGame() {
        var started = false;
        return new Operation(() => {
            if (!started) {
                started = true;
                _saved = false;
                Game1.NewDay(0f);
                return (false, null);
            }

            return (_saved, null);
        });
    }

    private static object? Quit() {
        Game1.quit = true;
        return null;
    }

    private static GameLocation ResolveLocation(string name) {
        return Game1.getLocationFromName(name)
            ?? throw new E2eRequestException($"location '{name}' not found");
    }

    private static Object ObjectAt(int x, int y, string locationName) {
        var location = ResolveLocation(locationName);
        if (!location.objects.TryGetValue(new Vector2(x, y), out var obj)) {
            throw new E2eRequestException("no object at that tile");
        }
        return obj;
    }

    private static ComputerStatefulDataContextEntry ComputerAt(int x, int y, string locationName) {
        var obj = ObjectAt(x, y, locationName);
        var heldModData = obj.heldObject.Value?.modData;
        if (heldModData is null || !heldModData.TryGetValue("ComputerId", out var id)) {
            throw new E2eRequestException("no computer at that tile");
        }
        if (!ModEntry.SharedContext.TryGetSingle<ComputerStatefulDataContextEntry>(id.AsId(), out var computer)) {
            throw new E2eRequestException("the computer entity is not registered");
        }
        return computer;
    }

    private static string? IdentityOf(Object obj) {
        var heldModData = obj.heldObject.Value?.modData;
        if (heldModData is not null && heldModData.TryGetValue("ComputerId", out var computerId)) {
            return computerId;
        }
        if (obj.modData.TryGetValue("RouterId", out var routerId)) {
            return routerId;
        }
        if (obj.modData.TryGetValue("PeripheralId", out var peripheralId)) {
            return peripheralId;
        }
        return null;
    }
}
