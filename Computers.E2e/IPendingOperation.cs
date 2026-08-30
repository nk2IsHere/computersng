namespace Computers.E2e;

// One control command in flight. Polled once per game tick until it reports completion.
public interface IPendingOperation {
    // Returns true when finished. On true either error is null and data carries the reply
    // payload, or error carries the failure text.
    bool TryComplete(out object? data, out string? error);
}
