using Computers.Computer.Boundary;

namespace Computers.Computer;

public class EventComputerApi : IComputerApi {
    public string Name => "Event";
    public bool ShouldExpose => true;
    public object Api => _state;
    
    public ISet<Type> ReceivableEvents => new HashSet<Type> {
        typeof(TickComputerEvent),
        typeof(KeyPressedEvent),
        typeof(MouseLeftClickedEvent),
        typeof(MouseRightClickedEvent), 
        typeof(MouseWheelEvent)
    };

    public ISet<Type> RegisterableApiTypes => new HashSet<Type> {
        typeof(EventComputerState),
        typeof(Event)
    };

    private readonly IComputerPort _computerPort;
    private readonly Configuration _configuration;
    
    private readonly EventComputerState _state;

    public EventComputerApi(IComputerPort computerPort, Configuration configuration) {
        _computerPort = computerPort;
        _configuration = configuration;
        
        _state = new EventComputerState();
    }

    public void ReceiveEvent(IComputerEvent computerEvent) {
       switch (computerEvent) {
           case TickComputerEvent tickComputerEvent:
               _state.Enqueue(new Event("Tick", new object[] { tickComputerEvent.Ticks }));
               break;
           case KeyPressedEvent keyPressedEvent:
               _state.Enqueue(new Event("KeyPressed", new object[] { keyPressedEvent.Key }));
               break;
           case MouseLeftClickedEvent mouseLeftClickedEvent:
               _state.Enqueue(new Event("MouseLeftClicked", new object[] { mouseLeftClickedEvent.X, mouseLeftClickedEvent.Y }));
               break;
           case MouseRightClickedEvent mouseRightClickedEvent:
               _state.Enqueue(new Event("MouseRightClicked", new object[] { mouseRightClickedEvent.X, mouseRightClickedEvent.Y }));
               break;
           case MouseWheelEvent mouseWheelEvent:
               _state.Enqueue(new Event("MouseWheel", new object[] { mouseWheelEvent.Direction }));
               break;
       }
    }

    public void Reset() {
        _state.Clear();
    }
}

internal record Event(string Type, object[] Data);

internal class EventComputerState {
    
    private readonly Queue<Event> _events = new();
    
    public void Enqueue(Event @event) {
        if (@event == null) {
            throw new ArgumentNullException(nameof(@event));
        }
        
        lock (_events) {
            _events.Enqueue(@event);
        }
    }
    
    public List<Event> Poll() {
        lock (_events) {
            var events = _events.ToList();
            _events.Clear();
            return events;
        }
    }
    
    public void Clear() {
        lock (_events) {
            _events.Clear();
        }
    }
}
