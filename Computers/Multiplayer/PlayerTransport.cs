namespace Computers.Multiplayer;

public record ChannelMessage(string? Json, byte[]? Bytes);

public interface IPlayerTransport {
    long LocalPlayerId { get; }

    bool IsHost { get; }

    long HostPlayerId { get; }

    void Send(long playerId, ChannelMessage message);

    event Action<long, ChannelMessage>? MessageReceived;
}
