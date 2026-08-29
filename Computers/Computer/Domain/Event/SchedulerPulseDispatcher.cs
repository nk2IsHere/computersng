using Computers.Game;

namespace Computers.Computer.Domain.Event;

public class SchedulerPulseDispatcher : IEventHandler {
    private readonly ComputerScheduler _scheduler;

    public SchedulerPulseDispatcher(ComputerScheduler scheduler) {
        _scheduler = scheduler;
    }

    public ISet<Type> EventTypes => new HashSet<Type> { typeof(UpdateTickedEvent) };

    public void Handle(IEvent @event) {
        _scheduler.Pulse();
    }
}
