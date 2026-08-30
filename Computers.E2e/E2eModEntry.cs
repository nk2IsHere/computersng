using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;

namespace Computers.E2e;

// The harness mod. It does nothing unless COMPUTERS_E2E is set, so it can stay
// installed beside the main mod without affecting normal play.
public class E2eModEntry : Mod {
    public override void Entry(IModHelper helper) {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPUTERS_E2E"))) {
            return;
        }

        var port = int.TryParse(Environment.GetEnvironmentVariable("COMPUTERS_E2E_PORT"), out var parsed) ? parsed : 6161;
        var operations = new E2eOperations();
        var service = new E2eControlService(port, operations.Create, message => Monitor.Log(message, LogLevel.Info));
        service.Start();
        helper.Events.GameLoop.UpdateTicked += (_, _) => {
            operations.NotifyTick();
            service.Tick();
        };
        helper.Events.GameLoop.Saved += (_, _) => operations.NotifySaved();
        helper.Events.GameLoop.SaveLoaded += (_, _) => operations.NotifySaveLoaded();

        var hostMode = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPUTERS_E2E_HOST"));

        var newSave = Environment.GetEnvironmentVariable("COMPUTERS_E2E_NEW_SAVE");
        if (!string.IsNullOrEmpty(newSave)) {
            RegisterNewSave(helper, newSave, hostMode);
        }

        var loadSave = Environment.GetEnvironmentVariable("COMPUTERS_E2E_LOAD_SAVE");
        if (!string.IsNullOrEmpty(loadSave)) {
            RegisterLoadSave(helper, loadSave, hostMode);
        }

        var joinAddress = Environment.GetEnvironmentVariable("COMPUTERS_E2E_JOIN");
        if (!string.IsNullOrEmpty(joinAddress)) {
            RegisterJoin(helper, joinAddress);
        }
    }

    // Creates a brand new farm at the title screen through the vanilla new game entry
    // point, the method the character creation screen calls when the player presses OK.
    private void RegisterNewSave(IModHelper helper, string saveName, bool hostMode) {
        TitleMenu.SkipSplashScreens = true;
        var triggered = false;
        helper.Events.GameLoop.UpdateTicked += (_, _) => {
            if (triggered || Game1.activeClickableMenu is not TitleMenu titleMenu) {
                return;
            }

            triggered = true;
            Monitor.Log($"E2e new save: creating farm '{saveName}'.", LogLevel.Info);
            if (hostMode) {
                Game1.multiplayerMode = 2;
                Game1.startingCabins = 1;
            }

            Game1.player.Name = "E2E";
            Game1.player.favoriteThing.Value = "e2e";
            Game1.player.farmName.Value = saveName;
            Game1.player.isCustomized.Value = true;
            titleMenu.createdNewCharacter(true);
        };
    }

    // Mirrors the main mod's dev autoload but reads only the e2e variable, so the
    // developer's dev saves are never touched by harness runs.
    private void RegisterLoadSave(IModHelper helper, string saveName, bool hostMode) {
        TitleMenu.SkipSplashScreens = true;
        var triggered = false;
        helper.Events.GameLoop.UpdateTicked += (_, _) => {
            if (triggered || Game1.activeClickableMenu is not TitleMenu) {
                return;
            }

            triggered = true;
            var savesPath = Constants.SavesPath;
            var saveFolder = Directory.Exists(savesPath)
                ? Directory.EnumerateDirectories(savesPath)
                    .Select(Path.GetFileName)
                    .OfType<string>()
                    .FirstOrDefault(name => name == saveName || name.StartsWith($"{saveName}_"))
                : null;

            if (saveFolder is null) {
                Monitor.Log($"E2e load: no save folder matching '{saveName}' found in {savesPath}.", LogLevel.Warn);
                return;
            }

            Monitor.Log($"E2e load: loading save '{saveFolder}'.", LogLevel.Info);
            if (hostMode) {
                Game1.multiplayerMode = 2;
            }
            // Close the title menu after starting the load, otherwise the lingering menu
            // resets the game back to the title screen.
            SaveGame.Load(saveFolder);
            Game1.exitActiveMenu();
        };
    }

    // Joins a LAN co-op host as a farmhand through the vanilla join flow.
    private void RegisterJoin(IModHelper helper, string address) {
        TitleMenu.SkipSplashScreens = true;
        var triggered = false;
        var slotActivated = false;
        helper.Events.GameLoop.UpdateTicked += (_, _) => {
            if (TitleMenu.subMenu is FarmhandMenu farmhandMenu) {
                if (!slotActivated && TryActivateFarmhandSlot(helper, farmhandMenu)) {
                    slotActivated = true;
                }
                return;
            }

            if (triggered || Game1.activeClickableMenu is not TitleMenu) {
                return;
            }

            // Mirrors the vanilla co-op join exactly. The join flow must not set the
            // client multiplayer mode itself, the vanilla path leaves it alone so the
            // world bootstrap inside the slot activation still sees a master game.
            triggered = true;
            Monitor.Log($"E2e join: connecting to {address}.", LogLevel.Info);
            var multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
            var client = multiplayer.InitClient(new LidgrenClient(address));
            TitleMenu.subMenu = new FarmhandMenu(client);
        };
    }

    private bool TryActivateFarmhandSlot(IModHelper helper, FarmhandMenu menu) {
        if (menu.gettingFarmhands || menu.approvingFarmhand) {
            return false;
        }

        var slots = helper.Reflection.GetField<List<LoadGameMenu.MenuSlot>>(menu, "menuSlots").GetValue();
        var slot = slots.OfType<FarmhandMenu.FarmhandSlot>().FirstOrDefault(s => !s.BelongsToAnotherPlayer());
        if (slot is null) {
            return false;
        }

        Monitor.Log("E2e join: activating the first farmhand slot.", LogLevel.Info);
        slot.Activate();
        return true;
    }
}
