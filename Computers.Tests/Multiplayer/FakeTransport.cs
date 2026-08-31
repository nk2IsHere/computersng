using Computers.Multiplayer;

namespace Computers.Tests.Multiplayer;

public sealed class FakeTransport : IPlayerTransport {
    public long LocalPlayerId { get; init; }
    public bool IsHost { get; init; }
    public long HostPlayerId { get; init; }
    public List<(long To, ChannelMessage Message)> Sent { get; } = new();

    public event Action<long, ChannelMessage>? MessageReceived;

    public void Send(long playerId, ChannelMessage message) {
        Sent.Add((playerId, message));
    }

    public void Deliver(long from, ChannelMessage message) {
        MessageReceived?.Invoke(from, message);
    }
}
