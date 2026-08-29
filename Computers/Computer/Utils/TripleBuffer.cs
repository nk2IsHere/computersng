namespace Computers.Computer.Utils;

public class TripleBuffer<T> where T : class {
    private readonly object _lock = new();

    private T _produce;
    private T _ready;
    private T _display;
    private bool _hasNewFrame;

    public TripleBuffer(T a, T b, T c) {
        _produce = a;
        _ready = b;
        _display = c;
    }

    public T ProduceSlot {
        get {
            lock (_lock) {
                return _produce;
            }
        }
    }

    public void Publish() {
        lock (_lock) {
            (_produce, _ready) = (_ready, _produce);
            _hasNewFrame = true;
        }
    }

    public (T Slot, bool IsNew) Consume() {
        lock (_lock) {
            if (!_hasNewFrame) {
                return (_display, false);
            }

            (_ready, _display) = (_display, _ready);
            _hasNewFrame = false;
            return (_display, true);
        }
    }
}
