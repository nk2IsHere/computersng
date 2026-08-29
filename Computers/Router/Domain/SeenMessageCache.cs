namespace Computers.Router.Domain;

public class SeenMessageCache {
    private readonly int _capacity;
    private readonly HashSet<Guid> _seen = new();
    private readonly Queue<Guid> _order = new();

    public SeenMessageCache(int capacity) {
        _capacity = capacity;
    }

    public bool Remember(Guid messageId) {
        lock (_seen) {
            if (!_seen.Add(messageId)) {
                return false;
            }

            _order.Enqueue(messageId);
            while (_order.Count > _capacity) {
                _seen.Remove(_order.Dequeue());
            }

            return true;
        }
    }

    public void Clear() {
        lock (_seen) {
            _seen.Clear();
            _order.Clear();
        }
    }
}
