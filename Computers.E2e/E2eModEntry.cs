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

        // Only one window can be focused, and the framework sleeps unfocused windows
        // every frame, which crawls whichever harness process is in the background.
        // E2e runs need every process at full speed regardless of focus.
        helper.Events.GameLoop.GameLaunched += (_, _) => {
            StardewValley.GameRunner.instance.InactiveSleepTime = TimeSpan.Zero;
        };

        var port = int.TryParse(Environment.GetEnvironmentVariable("COMPUTERS_E2E_PORT"), out var parsed) ? parsed : 6161;
        var operations = new E2eOperations();
        var service = new E2eControlService(port, operations.Create, message => Monitor.Log(message, LogLevel.Info));
        service.Start();
        helper.Events.GameLoop.UpdateTicked += (_, _) => {
            var screenId = StardewModdingAPI.Context.ScreenId;
            if (screenId == 0) {
                operations.NotifyTick();
            }
            service.Tick(screenId);
            TryActivateSplitScreenSlot(helper, screenId);
            TryCompleteFarmhandCustomization();
            TryDismissShippingMenu();
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
                // Two cabins so a split screen player and a remote farmhand fit at once.
                Game1.startingCabins = 2;
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
            var multiplayer = helper.Reflection.GetField<StardewValley.Multiplayer>(typeof(Game1), "multiplayer").GetValue();
            var client = multiplayer.InitClient(new LidgrenClient(address));
            TitleMenu.subMenu = new FarmhandMenu(client);
        };
    }

    // A split screen instance created by the harness lands on the farmhand menu of the
    // local server. The harness activates its first slot once, like the remote join.
    private readonly StardewModdingAPI.Utilities.PerScreen<bool> _splitSlotActivated = new(() => false);

    private void TryActivateSplitScreenSlot(IModHelper helper, int screenId) {
        if (screenId == 0 || _splitSlotActivated.Value) {
            return;
        }
        var menu = Game1.activeClickableMenu as FarmhandMenu ?? TitleMenu.subMenu as FarmhandMenu;
        if (menu is null) {
            return;
        }
        if (TryActivateFarmhandSlot(helper, menu)) {
            _splitSlotActivated.Value = true;
        }
    }

    // A brand new farmhand lands on the naming dialog before entering the world. The
    // harness fills the mandatory fields and presses ok so joins complete unattended.
    private static void TryCompleteFarmhandCustomization() {
        if (Game1.activeClickableMenu is not CharacterCustomization customization) {
            return;
        }
        if (customization.source != CharacterCustomization.Source.NewFarmhand) {
            return;
        }
        if (string.IsNullOrEmpty(Game1.player.Name)) {
            Game1.player.Name = "E2EHand";
        }
        if (string.IsNullOrEmpty(Game1.player.favoriteThing.Value)) {
            Game1.player.favoriteThing.Value = "e2e";
        }
        customization.receiveLeftClick(customization.okButton.bounds.Center.X, customization.okButton.bounds.Center.Y);
    }

    // The end of day is synchronized across players and every one of them gets a
    // shipping summary that blocks until its ok button is clicked. This runs on every
    // screen of every harness driven process so day changes complete unattended.
    private static void TryDismissShippingMenu() {
        if (Game1.activeClickableMenu is not StardewValley.Menus.ShippingMenu shippingMenu) {
            return;
        }
        var ok = shippingMenu.okButton.bounds.Center;
        shippingMenu.receiveLeftClick(ok.X, ok.Y);
    }

    private bool TryActivateFarmhandSlot(IModHelper helper, FarmhandMenu menu) {
        if (menu.gettingFarmhands || menu.approvingFarmhand) {
            return false;
        }

        var slots = helper.Reflection.GetField<List<LoadGameMenu.MenuSlot>>(menu, "menuSlots").GetValue();
        // A slot whose farmer is online belongs to a connected player, claiming it gets
        // rejected by the server, so the picker skips those.
        var slot = slots.OfType<FarmhandMenu.FarmhandSlot>()
            .FirstOrDefault(s => !s.BelongsToAnotherPlayer() && s.Farmer?.isActive() != true);
        if (slot is null) {
            return false;
        }

        // A fresh farmhand slot would show the naming dialog after the world loads. In
        // split screen that dialog deadlocks the instance for the harness, because the
        // local new day process suspends the instance's mod events until the dialog is
        // done and only raw input can finish it. Customizing the farmer before the
        // activation skips the dialog entirely.
        if (slot.Farmer is { } farmer && !farmer.isCustomized.Value) {
            farmer.Name = "E2EHand";
            farmer.favoriteThing.Value = "e2e";
            farmer.isCustomized.Value = true;
        }

        Monitor.Log("E2e join: activating the first farmhand slot.", LogLevel.Info);
        slot.Activate();
        return true;
    }
}
