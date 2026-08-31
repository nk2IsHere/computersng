using System.Collections.Concurrent;
using Computers.Multiplayer.Domain.Wire;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;

namespace Computers.Multiplayer.Domain;

// The host side of the player channel. Handlers are registered per command, incoming
// requests queue on arrival and execute only inside Tick on the caller's thread.
public sealed class HostChannel {
    private readonly IPlayerTransport _transport;
    private readonly Action<string> _log;
    private readonly Dictionary<string, Func<long, ChannelRequest, object?>> _handlers = new();
    private readonly ConcurrentQueue<(long From, ChannelMessage Message)> _incoming = new();

    public HostChannel(IPlayerTransport transport, Action<string> log) {
        _transport = transport;
        _log = log;
        transport.MessageReceived += (from, message) => _incoming.Enqueue((from, message));
    }

    public void Register(string cmd, Func<long, ChannelRequest, object?> handler) {
        _handlers[cmd] = handler;
    }

    public void Tick() {
        while (_incoming.TryDequeue(out var entry)) {
            if (entry.Message.Json is null) {
                continue;
            }
            HandleRequest(entry.From, entry.Message.Json);
        }
    }

    private void HandleRequest(long from, string json) {
        JObject payload;
        try {
            payload = JObject.Parse(json);
        } catch (Exception) {
            _log("Channel request was not valid JSON.");
            return;
        }

        // Replies and events can echo back through an in process transport that fans
        // messages out to every channel. Answering them would answer our own replies
        // in an endless loop, so anything that is not a request drops silently.
        if (payload["re"] is not null || payload["event"] is not null) {
            return;
        }

        WireRequests.TryReadCid(payload, out var cid);
        var cmd = payload["cmd"]?.Value<string>();
        if (cmd is null) {
            _log("Channel request without a command was dropped.");
            return;
        }

        var version = payload["v"]?.Value<int>() ?? -1;
        if (version != ChannelProtocol.Version) {
            Reply(from, Router.Domain.Wire.Reply.Failure(cid, "protocol version mismatch, update the mod"));
            return;
        }

        try {
            if (!_handlers.TryGetValue(cmd, out var handler)) {
                throw new ChannelRequestException($"unknown command '{cmd}'");
            }
            var request = ChannelRequestParser.Parse(cmd, payload);
            Reply(from, Router.Domain.Wire.Reply.Success(cid, handler(from, request)));
        } catch (ChannelRequestException exception) {
            Reply(from, Router.Domain.Wire.Reply.Failure(cid, exception.Message));
        } catch (Exception exception) {
            _log($"Channel handler for '{cmd}' threw. {exception}");
            Reply(from, Router.Domain.Wire.Reply.Failure(cid, "the host failed to handle the request"));
        }
    }

    private void Reply(long playerId, Reply reply) {
        _transport.Send(playerId, new ChannelMessage(WireJson.Serialize(reply), null));
    }

    public void SendEvent(long playerId, object eventPayload) {
        _transport.Send(playerId, new ChannelMessage(WireJson.Serialize(eventPayload), null));
    }

    public void SendFrame(long playerId, string computerId, bool snapshot, byte[] body) {
        var header = WireJson.Serialize(new FrameEventHeader("frame", computerId, snapshot));
        _transport.Send(playerId, new ChannelMessage(header, body));
    }
}
