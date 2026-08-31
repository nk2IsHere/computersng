using Computers.MachineController.Domain;
using Computers.Peripheral.Domain;
using Computers.Router.Domain;
using Computers.ShippingController;
using Computers.ShippingController.Domain.Wire;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace Computers.Game.Domain;

// The Stardew implementation of the shipping controller's world port.
public class StardewShippingWorld : IShippingWorld {
    public IShippingLocation? LocationOf(Placement placement) {
        var location = Game1.getLocationFromName(placement.Location);
        return location is null ? null : new StardewShippingLocation(location);
    }

    public int? Price(string itemId) {
        var item = ItemRegistry.Create(itemId, 1, 0, allowNull: true);
        return item?.sellToStorePrice();
    }

    private class StardewShippingLocation : IShippingLocation {
        private readonly GameLocation _location;

        public StardewShippingLocation(GameLocation location) {
            _location = location;
        }

        public IGroupWorld GroupWorld => new StardewGroupWorld(_location);

        public IShippingAccess Access(MachineGroup group) {
            return new StardewShippingAccess(_location, group);
        }
    }

    private class StardewShippingAccess : IShippingAccess {
        private readonly GameLocation _location;
        private readonly MachineGroup _group;

        public StardewShippingAccess(GameLocation location, MachineGroup group) {
            _location = location;
            _group = group;
        }

        public IReadOnlyList<ShippedChest> Chests() {
            return GroupChests()
                .Select(entry => new ShippedChest(
                    entry.X,
                    entry.Y,
                    entry.Chest.Items
                        .Where(item => item is not null)
                        .Select(item => new ShippedChestItem(item.ItemId, item.Name, item.Stack))
                        .ToList()
                ))
                .ToList();
        }

        public SellResult Sell(string itemId, int? count) {
            var bin = Game1.getFarm().getShippingBin(Game1.player);
            var remaining = count ?? int.MaxValue;
            var sold = 0;
            var value = 0;
            string? name = null;

            foreach (var (_, _, chest) in GroupChests()) {
                for (var slot = chest.Items.Count - 1; slot >= 0 && remaining > 0; slot--) {
                    var item = chest.Items[slot];
                    if (item is null || (item.ItemId != itemId && item.QualifiedItemId != itemId)) {
                        continue;
                    }

                    var take = Math.Min(item.Stack, remaining);
                    var shipped = item.getOne();
                    shipped.Stack = take;
                    bin.Add(shipped);
                    Game1.getFarm().lastItemShipped = shipped;

                    name ??= item.Name;
                    sold += take;
                    value += shipped.sellToStorePrice() * take;
                    remaining -= take;

                    if (take >= item.Stack) {
                        chest.Items[slot] = null;
                    }
                    else {
                        item.Stack -= take;
                    }
                }
            }

            if (sold == 0) {
                throw new PeripheralRequestException($"item '{itemId}' not found in group chests");
            }

            return new SellResult(itemId, name!, sold, value);
        }

        private IEnumerable<(int X, int Y, Chest Chest)> GroupChests() {
            foreach (var (x, y) in _group.Chests) {
                if (_location.objects.TryGetValue(new Vector2(x, y), out var obj) && obj is Chest chest) {
                    yield return (x, y, chest);
                }
            }
        }
    }
}
