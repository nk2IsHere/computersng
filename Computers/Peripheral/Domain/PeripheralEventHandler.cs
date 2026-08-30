using Computers.Core;
using Computers.Game;

namespace Computers.Peripheral.Domain;

public abstract class PeripheralEventHandler : IEventHandler {

    private readonly ContextLookup<IPeripheralPort> _peripherals;

    protected PeripheralEventHandler(ContextLookup<IPeripheralPort> peripherals) {
        _peripherals = peripherals;
    }

    public abstract ISet<Type> EventTypes { get; }

    public void Handle(IEvent @event) {
        var peripheralEvent = CreateEvent(@event);
        _peripherals
            .Get()
            .Where(peripheral => peripheralEvent.BelongsTo(peripheral.Id))
            .ForEach(peripheral => peripheral.Value.Fire(peripheralEvent));
    }

    protected abstract IPeripheralEvent CreateEvent(IEvent @event);
}
