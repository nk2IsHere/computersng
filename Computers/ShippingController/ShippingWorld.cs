using Computers.MachineController.Domain;
using Computers.Router.Domain;
using Computers.ShippingController.Domain.Wire;

namespace Computers.ShippingController;

public interface IShippingWorld {
    IShippingLocation? LocationOf(Placement placement);

    int? Price(string itemId);
}

public interface IShippingLocation {
    IGroupWorld GroupWorld { get; }

    IShippingAccess Access(MachineGroup group);
}

public interface IShippingAccess {
    IReadOnlyList<ShippedChest> Chests();
    
    SellResult Sell(string itemId, int? count);
}
