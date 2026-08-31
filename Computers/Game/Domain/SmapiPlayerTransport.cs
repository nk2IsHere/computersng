using System.Collections.Concurrent;
using Computers.Multiplayer;
using StardewModdingAPI;
using StardewValley;

namespace Computers.Game.Domain;

public record TransportEnvelope(string? Json, byte[]? Bytes);

public class SmapiPlayerTransport : IPlayerTransport {
    private const string MessageType = "channel";

    private readonly IModHelper _helper;
    private readonly string _modId;

    public SmapiPlayerTransport(IModHelper helper, IManifest manifest) {
        _helper = helper;
        _modId = manifest.UniqueID;
        helper.Events.Multiplayer.ModMessageReceived += (_, args) => {
            if (args.FromModID != _modId || args.Type != MessageType) {
                return;
            }
            var envelope = args.ReadAs<TransportEnvelope>();
            MessageReceived?.Invoke(args.FromPlayerID, new ChannelMessage(envelope.Json, envelope.Bytes));
        };
    }

    public long LocalPlayerId => Game1.player.UniqueMultiplayerID;

    public bool IsHost => Game1.IsMasterGame && Context.ScreenId == 0;

    public long HostPlayerId => Game1.MasterPlayer.UniqueMultiplayerID;

    public event Action<long, ChannelMessage>? MessageReceived;

    private readonly ConcurrentDictionary<int, long> _localPlayersByScreen = new();

    public void NoteLocalPlayer() {
        _localPlayersByScreen[Context.ScreenId] = Game1.player.UniqueMultiplayerID;
    }

    public void Send(long playerId, ChannelMessage message) {
        if (_localPlayersByScreen.Values.Contains(playerId)) {
            MessageReceived?.Invoke(LocalPlayerId, message);
            return;
        }

        _helper.Multiplayer.SendMessage(
            new TransportEnvelope(message.Json, message.Bytes),
            MessageType,
            new[] { _modId },
            new[] { playerId }
        );
    }
}
