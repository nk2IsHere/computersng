using System.Collections.Concurrent;
using Computers.Multiplayer.Domain.Wire;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;

namespace Computers.Multiplayer.Domain;

public sealed class ClientChannel {
    private sealed class PendingCall {
        public PendingCall(string cmd, Action<JObject?> onSuccess, Action<string> onError, int ticksLeft) {
            Cmd = cmd;
            OnSuccess = onSuccess;
            OnError = onError;
            TicksLeft = ticksLeft;
        }

        public string Cmd { get; }
        public Action<JObject?> OnSuccess { get; }
        public Action<string> OnError { get; }
        public int TicksLeft { get; set; }
    }

    private readonly IPlayerTransport _transport;
    private readonly int _callTimeoutTicks;
    private readonly Action<string> _log;
    private readonly ConcurrentQueue<(long From, ChannelMessage Message)> _incoming = new();
    private readonly Dictionary<string, PendingCall> _pending = new();
    private int _nextCid = 1;

    public event Action<JObject>? EventReceived;
    public event Action<string, bool, byte[]>? FrameReceived;

    public ClientChannel(IPlayerTransport transport, int callTimeoutTicks, Action<string> log) {
        _transport = transport;
        _callTimeoutTicks = callTimeoutTicks;
        _log = log;
        transport.MessageReceived += (from, message) => _incoming.Enqueue((from, message));
    }

    public void Send(string cmd, object args) {
        _transport.Send(_transport.HostPlayerId, new ChannelMessage(Envelope(cmd, args, $"f{_nextCid++}"), null));
    }

    public void Call(string cmd, object args, Action<JObject?> onSuccess, Action<string> onError) {
        var cid = $"q{_nextCid++}";
        _pending[cid] = new PendingCall(cmd, onSuccess, onError, _callTimeoutTicks);
        _transport.Send(_transport.HostPlayerId, new ChannelMessage(Envelope(cmd, args, cid), null));
    }

    private static string Envelope(string cmd, object args, string cid) {
        var payload = JObject.FromObject(args);
        payload["cid"] = cid;
        payload["cmd"] = cmd;
        payload["v"] = ChannelProtocol.Version;
        return payload.ToString(Newtonsoft.Json.Formatting.None);
    }

    public void Tick() {
        while (_incoming.TryDequeue(out var entry)) {
            if (entry.From != _transport.HostPlayerId || entry.Message.Json is null) {
                continue;
            }
            Handle(entry.Message);
        }

        foreach (var cid in _pending.Keys.ToList()) {
            var pending = _pending[cid];
            if (--pending.TicksLeft > 0) {
                continue;
            }
            _pending.Remove(cid);
            pending.OnError($"call '{pending.Cmd}' timed out");
        }
    }

    private void Handle(ChannelMessage message) {
        JObject payload;
        try {
            payload = JObject.Parse(message.Json!);
        } catch (Exception) {
            _log("Channel message from the host was not valid JSON.");
            return;
        }

        if (payload["event"]?.Value<string>() is { } eventName) {
            if (eventName == "frame" && message.Bytes is not null) {
                FrameReceived?.Invoke(
                    payload["computerId"]!.Value<string>()!,
                    payload["snapshot"]?.Value<bool>() ?? false,
                    message.Bytes
                );
            } else {
                EventReceived?.Invoke(payload);
            }
            return;
        }

        var re = payload["re"]?.Value<string>();
        if (re is null || !_pending.Remove(re, out var pending)) {
            return;
        }

        if (payload["ok"]?.Value<bool>() == true) {
            pending.OnSuccess(payload["data"] as JObject);
        } else {
            pending.OnError(payload["error"]?.Value<string>() ?? "unknown error");
        }
    }
}
