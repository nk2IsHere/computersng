using Computers.Computer;
using Computers.Core;
using Computers.MachineController.Domain;
using Computers.Peripheral;
using Computers.Peripheral.Domain;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Router.Domain.Wire;
using Computers.ShippingController.Domain.Wire;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace Computers.ShippingController.Domain;

public class ShippingControllerStatefulDataContextEntry : PeripheralEntity<ShippingControllerStatefulDataContextEntry> {

    private readonly IShippingWorld _shippingWorld;
    private readonly PeripheralTier _tier;

    private MachineGroup? _cachedGroup;
    private volatile bool _groupDirty = true;
    private IShippingAccess? _access;

    public ShippingControllerStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        IShippingWorld shippingWorld,
        PeripheralTier tier
    ) : base(factoryId, id, monitor, configuration, registry, routers) {
        _shippingWorld = shippingWorld;
        _tier = tier;
    }

    public override void NotifyWorldChanged() {
        _groupDirty = true;
    }

    protected override void BeforeCommands() {
        var placement = Registry.PlacementOf(Id);
        var location = placement is null ? null : _shippingWorld.LocationOf(placement);
        if (placement is null || location is null) {
            _access = null;
            return;
        }

        if (_groupDirty || _cachedGroup is null) {
            _cachedGroup = MachineGroupScanner.Scan(
                location.GroupWorld,
                placement.X,
                placement.Y,
                Configuration.MachineController.MaxGroupSize,
                _tier == PeripheralTier.Basic ? 1 : int.MaxValue
            );
            _groupDirty = false;
        }

        _access = location.Access(_cachedGroup);
    }

    protected override Reply? ProcessRequest(string sourceAddress, JObject request) {
        if (!WireRequests.TryReadCid(request, out var cid)) {
            return null;
        }

        try {
            return Reply.Success(cid, Dispatch(ShippingRequestParser.ParseBody(request)));
        } catch (PeripheralRequestException exception) {
            return Reply.Failure(cid, exception.Message);
        }
    }

    private object Dispatch(ShippingRequest request) {
        switch (request) {
            case ShippingPingRequest:
                return new TieredPingResult("shippingController", _tier);

            case ShippingListRequest:
                return new ChestListing(Access().Chests());

            case PriceRequest price:
                return new PriceResult(
                    price.ItemId,
                    _shippingWorld.Price(price.ItemId)
                        ?? throw new PeripheralRequestException($"unknown item '{price.ItemId}'")
                );

            case SellRequest sell:
                return Access().Sell(sell.ItemId, sell.Count);

            default:
                throw new PeripheralRequestException($"unknown command '{request.GetType().Name}'");
        }
    }

    private IShippingAccess Access() {
        return _access ?? throw new PeripheralRequestException("controller has no world position");
    }
}
