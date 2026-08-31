namespace Computers.Multiplayer.Domain.Wire;

public static class ChannelProtocol {
    public const int Version = 1;
}

public class ChannelRequestException : Exception {
    public ChannelRequestException(string error) : base(error) {
    }
}

public abstract record ChannelRequest;

public record OpenScreenRequest(int X, int Y, string Location) : ChannelRequest;

public record CloseScreenRequest(string ComputerId) : ChannelRequest;

// Kind is one of key, leftClick, rightClick and wheel. A and B carry the key code or
// the coordinates depending on the kind.
public record ScreenInputRequest(string ComputerId, string Kind, int A, int B) : ChannelRequest;

public record InitializeComputerRequest(int X, int Y, string Location) : ChannelRequest;

public record OpenScreenResult(string ComputerId, int CanvasWidth, int CanvasHeight);

public record ScreenClosedEvent(string Event, string ComputerId);

// Binary frame body travels alongside this header in the same channel message.
public record FrameEventHeader(string Event, string ComputerId, bool Snapshot);
