namespace Computers.E2e.Wire;

public class E2eRequestException : Exception {
    public E2eRequestException(string error) : base(error) {
    }
}

public abstract record E2eRequest;

public record StatusRequest : E2eRequest;

public record PlaceRequest(string Item, int X, int Y, string Location) : E2eRequest;

public record ChestItem(string ItemId, int Count);

public record PlaceChestRequest(int X, int Y, string Location, IReadOnlyList<ChestItem> Items) : E2eRequest;

public record RemoveRequest(int X, int Y, string Location) : E2eRequest;

public record WriteDiskRequest(int X, int Y, string Location, string Path, string Content) : E2eRequest;

public record ReadDiskRequest(int X, int Y, string Location, string Path) : E2eRequest;

public record RestartComputerRequest(int X, int Y, string Location) : E2eRequest;

public record InteractRequest(int X, int Y, string Location) : E2eRequest;

public record QueryObjectRequest(int X, int Y, string Location) : E2eRequest;

public record WaitTicksRequest(int Count) : E2eRequest;

public record QueryShippingBinRequest : E2eRequest;

public record SaveGameRequest : E2eRequest;

public record QuitRequest : E2eRequest;

public record StatusResult(string GameMode, bool SaveLoaded, int PlayerCount, bool WorldReady);

public record PlaceResult(string Id);

public record ReadDiskResult(string Content);

public record ShippedItem(string ItemId, string Name, int Count);

public record ShippingBinResult(IReadOnlyList<ShippedItem> Items);

public record ObjectResult(string ItemId, string? Id, IReadOnlyList<ShippedItem>? ChestItems);
