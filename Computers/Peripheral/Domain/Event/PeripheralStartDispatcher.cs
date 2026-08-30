using Computers.Core;
using Computers.Game;

namespace Computers.Peripheral.Domain.Event;

public class PeripheralStartDispatcher : PeripheralEventHandler {

    public PeripheralStartDispatcher(ContextLookup<IPeripheralPort> peripherals) : base(peripherals) {
    }

    public override ISet<Type> EventTypes => new HashSet<Type> { typeof(SaveLoadedEvent) };

    protected override IPeripheralEvent CreateEvent(IEvent @event) {
        return new StartPeripheralEvent();
    }
}
