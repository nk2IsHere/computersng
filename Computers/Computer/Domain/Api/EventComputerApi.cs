using Computers.Game;
using StardewModdingAPI;

namespace Computers.Computer.Domain.Api;

public class EventComputerApi : IComputerApi {
    public string Name => "Event";
    public bool ShouldExpose => true;
    public object Api => _state;
    
    private readonly ISet<SButton> _heldButtons = new HashSet<SButton>();
    
    public ISet<Type> ReceivableEvents => new HashSet<Type> {
        typeof(TickComputerEvent),
        typeof(KeyPressedEvent),
        typeof(MouseLeftClickedEvent),
        typeof(MouseRightClickedEvent), 
        typeof(MouseWheelEvent),
        typeof(ButtonHeldEvent),
        typeof(ButtonUnheldEvent)
    };

    public IRedundantLoader? LibraryLoader => null;

    private readonly IComputerPort _computerPort;
    private readonly Configuration _configuration;
    
    private readonly EventComputerState _state;

    public EventComputerApi(IComputerPort computerPort) {
        _computerPort = computerPort;
        _configuration = computerPort.Configuration;
        
        _state = new EventComputerState(computerPort);
    }

    public void ReceiveEvent(IComputerEvent computerEvent) {
       switch (computerEvent) {
           case ButtonHeldEvent buttonHeldEvent:
               _heldButtons.Add(buttonHeldEvent.Key);
               _state.Enqueue(new EventEntry("ButtonHeld", new object[] { (int) buttonHeldEvent.Key }));
               break;
            case ButtonUnheldEvent buttonUnheldEvent:
                _heldButtons.Remove(buttonUnheldEvent.Key);
                _state.Enqueue(new EventEntry("ButtonUnheld", new object[] { (int) buttonUnheldEvent.Key }));
                break;
           case TickComputerEvent tickComputerEvent:
               _state.Enqueue(new EventEntry("Tick", new object[] { tickComputerEvent.Ticks }));
               break;
           case KeyPressedEvent keyPressedEvent:
               _state.Enqueue(new EventEntry("KeyPressed", new object[] {
                   (int) keyPressedEvent.Key,
                   _heldButtons
                       .Cast<int>()
                       .ToArray()
               }));
               break;
           case MouseLeftClickedEvent mouseLeftClickedEvent:
               _state.Enqueue(new EventEntry("MouseLeftClicked", new object[] { mouseLeftClickedEvent.X, mouseLeftClickedEvent.Y }));
               break;
           case MouseRightClickedEvent mouseRightClickedEvent:
               _state.Enqueue(new EventEntry("MouseRightClicked", new object[] { mouseRightClickedEvent.X, mouseRightClickedEvent.Y }));
               break;
           case MouseWheelEvent mouseWheelEvent:
               _state.Enqueue(new EventEntry("MouseWheel", new object[] { mouseWheelEvent.Direction }));
               break;
       }
    }

    public void Reset() {
        _state.Clear();
    }
}

internal record EventEntry(string Type, object[] Data);

internal class EventComputerState {
    
    private readonly IComputerPort _computerPort;
    private readonly Queue<EventEntry> _events = new();

    public EventComputerState(IComputerPort computerPort) {
        _computerPort = computerPort;
    }

    public void Enqueue(EventEntry @event) {
        if (@event == null) {
            throw new ArgumentNullException(nameof(@event));
        }
        
        lock (_events) {
            _events.Enqueue(@event);
        }
    }
    
    public List<EventEntry> Poll() {
        // Since the engine expects main function to be an infinite loop, we need to process tasks somewhere, 
        // where the code is executed every frame.
        // Polling events is a good place to do that, unless any external script decides not to poll events.
        // There is an explicit method to process tasks, so it can be called from the main function in such cases, but
        // generally it won't be necessary.
        _computerPort.ProcessTasks();
        
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
