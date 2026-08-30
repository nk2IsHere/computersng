using Computers.Core;
using Computers.Game;
using StardewModdingAPI.Events;

namespace Computers.Peripheral.Domain.Event;

public class PeripheralTickDispatcher : PeripheralEventHandler {

    public PeripheralTickDispatcher(ContextLookup<IPeripheralPort> peripherals) : base(peripherals) {
    }

    public override ISet<Type> EventTypes => new HashSet<Type> { typeof(UpdateTickedEvent) };

    protected override IPeripheralEvent CreateEvent(IEvent @event) {
        var args = @event.Data<UpdateTickedEventArgs>();
        return new TickPeripheralEvent(args.Ticks);
    }
}
