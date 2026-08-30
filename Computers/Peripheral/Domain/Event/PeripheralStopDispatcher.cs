using Computers.Core;
using Computers.Game;

namespace Computers.Peripheral.Domain.Event;

public class PeripheralStopDispatcher : PeripheralEventHandler {

    public PeripheralStopDispatcher(ContextLookup<IPeripheralPort> peripherals) : base(peripherals) {
    }

    public override ISet<Type> EventTypes => new HashSet<Type> { typeof(ReturnedToTitleEvent) };

    protected override IPeripheralEvent CreateEvent(IEvent @event) {
        return new StopPeripheralEvent();
    }
}
