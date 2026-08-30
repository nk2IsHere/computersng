using Computers.Computer;
using Computers.Core;
using Computers.Peripheral;
using Computers.Router;
using Computers.Router.Domain;
using StardewModdingAPI;

namespace Computers.ShippingController.Domain;

public class ShippingControllerStatefulDataContextEntryFactory : IStatefulDataContextEntryFactory {

    private readonly Id _basePeripheralId;

    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<IRouterPort> _routers;
    private readonly IShippingWorld _shippingWorld;
    private readonly PeripheralTier _tier;

    public ShippingControllerStatefulDataContextEntryFactory(
        Id id,
        Id basePeripheralId,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        IShippingWorld shippingWorld,
        PeripheralTier tier
    ) {
        FactoryId = id;
        _basePeripheralId = basePeripheralId;
        _monitor = monitor;
        _configuration = configuration;
        _registry = registry;
        _routers = routers;
        _shippingWorld = shippingWorld;
        _tier = tier;
    }

    public Id FactoryId { get; }

    public IContextEntry ProduceValue() {
        return ProduceValue(ContextEntryState.Empty);
    }

    public IContextEntry ProduceValue(ContextEntryState state) {
        var id = state.Id ?? _basePeripheralId / Id.Random();
        return new ShippingControllerStatefulDataContextEntry(
            FactoryId,
            id,
            _monitor,
            _configuration,
            _registry,
            _routers,
            _shippingWorld,
            _tier
        );
    }
}
