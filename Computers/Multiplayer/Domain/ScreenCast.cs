using Computers.Multiplayer.Domain.Wire;

namespace Computers.Multiplayer.Domain;

public sealed class ScreenCast : IScreenCastSink {
    private sealed class ComputerCast {
        public HashSet<long> Viewers { get; } = new();
        public HashSet<long> NeedSnapshot { get; } = new();
        public FramePayload? Pending { get; set; }
        public IReadOnlyList<FrameCommand> CachedCommands { get; set; } = Array.Empty<FrameCommand>();
        public RawLayer? CachedBackground { get; set; }
        public RawLayer? CachedForeground { get; set; }
        public int TicksUntilSend { get; set; }
    }

    private readonly HostChannel _channel;
    private readonly int _castTicksPerFrame;
    private readonly int _maxViewers;
    private readonly Action<string> _log;
    private readonly object _lock = new();
    private readonly Dictionary<string, ComputerCast> _casts = new();
    private volatile HashSet<string> _watchedComputers = new();

    public ScreenCast(HostChannel channel, int castTicksPerFrame, int maxViewers, Action<string> log) {
        _channel = channel;
        _castTicksPerFrame = Math.Max(1, castTicksPerFrame);
        _maxViewers = maxViewers;
        _log = log;
    }

    public bool HasViewers(string computerId) {
        return _watchedComputers.Contains(computerId);
    }

    public void PublishFrame(string computerId, FramePayload payload, int rawVersion) {
        lock (_lock) {
            if (!_casts.TryGetValue(computerId, out var cast) || cast.Viewers.Count == 0) {
                return;
            }
            cast.Pending = payload;
            cast.CachedCommands = payload.Commands;
            cast.CachedBackground = payload.Background ?? cast.CachedBackground;
            cast.CachedForeground = payload.Foreground ?? cast.CachedForeground;
        }
    }

    public OpenScreenResult Subscribe(string computerId, long playerId, int canvasWidth, int canvasHeight) {
        lock (_lock) {
            if (!_casts.TryGetValue(computerId, out var cast)) {
                cast = new ComputerCast();
                _casts[computerId] = cast;
            }
            if (!cast.Viewers.Contains(playerId) && cast.Viewers.Count >= _maxViewers) {
                throw new ChannelRequestException("viewer limit reached");
            }
            cast.Viewers.Add(playerId);
            cast.NeedSnapshot.Add(playerId);
            RebuildWatched();
        }
        return new OpenScreenResult(computerId, canvasWidth, canvasHeight);
    }

    public void Unsubscribe(string computerId, long playerId) {
        lock (_lock) {
            if (_casts.TryGetValue(computerId, out var cast)) {
                cast.Viewers.Remove(playerId);
                cast.NeedSnapshot.Remove(playerId);
                if (cast.Viewers.Count == 0) {
                    _casts.Remove(computerId);
                }
            }
            RebuildWatched();
        }
    }

    public void DropPlayer(long playerId) {
        lock (_lock) {
            foreach (var computerId in _casts.Keys.ToList()) {
                Unsubscribe(computerId, playerId);
            }
        }
    }

    public void DropComputer(string computerId) {
        List<long> viewers;
        lock (_lock) {
            if (!_casts.Remove(computerId, out var cast)) {
                return;
            }
            viewers = cast.Viewers.ToList();
            RebuildWatched();
        }
        foreach (var playerId in viewers) {
            _channel.SendEvent(playerId, new ScreenClosedEvent("screenClosed", computerId));
        }
    }

    public void Tick() {
        List<(string ComputerId, long PlayerId, bool Snapshot, FramePayload Payload)> outgoing = new();
        lock (_lock) {
            foreach (var (computerId, cast) in _casts) {
                if (cast.TicksUntilSend > 0) {
                    cast.TicksUntilSend--;
                }
                if (cast.TicksUntilSend > 0) {
                    continue;
                }

                var snapshotPayload = new FramePayload(cast.CachedCommands, cast.CachedBackground, cast.CachedForeground);
                var sentAnything = false;

                foreach (var playerId in cast.Viewers) {
                    if (cast.NeedSnapshot.Contains(playerId)) {
                        outgoing.Add((computerId, playerId, true, snapshotPayload));
                        sentAnything = true;
                    } else if (cast.Pending is not null) {
                        outgoing.Add((computerId, playerId, false, cast.Pending));
                        sentAnything = true;
                    }
                }

                cast.NeedSnapshot.Clear();
                cast.Pending = null;
                if (sentAnything) {
                    cast.TicksUntilSend = _castTicksPerFrame;
                }
            }
        }

        foreach (var (computerId, playerId, snapshot, payload) in outgoing) {
            try {
                _channel.SendFrame(playerId, computerId, snapshot, FrameCodec.Encode(payload));
            } catch (Exception exception) {
                _log($"Frame send to player {playerId} failed. {exception.Message}");
            }
        }
    }

    private void RebuildWatched() {
        _watchedComputers = _casts.Where(pair => pair.Value.Viewers.Count > 0).Select(pair => pair.Key).ToHashSet();
    }
}
