using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;

namespace Computers.ShippingController.Domain.Wire;

public record ShippedChestItem(string Id, string Name, int Count);

public record ShippedChest(int X, int Y, IReadOnlyList<ShippedChestItem> Items);

public record ChestListing(IReadOnlyList<ShippedChest> Chests);

public record PriceResult(string ItemId, int Price);

public record SellResult(string ItemId, string Name, int Sold, int Value);

public abstract record ShippingRequest;

public record ShippingPingRequest : ShippingRequest;

public record ShippingListRequest : ShippingRequest;

public record PriceRequest(string ItemId) : ShippingRequest;

public record SellRequest(string ItemId, int? Count) : ShippingRequest;
