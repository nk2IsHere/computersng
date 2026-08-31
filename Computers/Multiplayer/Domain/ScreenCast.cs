using Computers.Computer;
using Computers.Computer.Domain.Api;
using Computers.Multiplayer.Domain.Wire;
using Microsoft.Xna.Framework;

namespace Computers.Multiplayer.Domain;

public sealed class ScreenCast : IFrameTap {
    private sealed record CapturedFrame(IReadOnlyList<IRenderCommand> Commands, RawLayer? Background, RawLayer? Foreground);

    private sealed class ComputerCast {
        public HashSet<long> Viewers { get; } = new();
        public HashSet<long> NeedSnapshot { get; } = new();
        public CapturedFrame? Pending { get; set; }
        public IReadOnlyList<IRenderCommand> CachedCommands { get; set; } = Array.Empty<IRenderCommand>();
        public RawLayer? CachedBackground { get; set; }
        public RawLayer? CachedForeground { get; set; }
        public int? LastRawVersion { get; set; }
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

    public bool WantsFrames(string computerId) {
        return _watchedComputers.Contains(computerId);
    }

    public void OnFrame(
        string computerId,
        IReadOnlyList<IRenderCommand> commands,
        Color[] background,
        Color[] foreground,
        int rawVersion
    ) {
        bool includeRaw;
        lock (_lock) {
            if (!_casts.TryGetValue(computerId, out var cast) || cast.Viewers.Count == 0) {
                return;
            }
            includeRaw = rawVersion != cast.LastRawVersion;
        }

        var frame = new CapturedFrame(
            commands.ToList(),
            includeRaw ? RawLayer.From(background) : null,
            includeRaw ? RawLayer.From(foreground) : null
        );

        lock (_lock) {
            if (!_casts.TryGetValue(computerId, out var cast)) {
                return;
            }
            cast.Pending = frame;
            cast.CachedCommands = frame.Commands;
            cast.CachedBackground = frame.Background ?? cast.CachedBackground;
            cast.CachedForeground = frame.Foreground ?? cast.CachedForeground;
            cast.LastRawVersion = rawVersion;
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
        List<(string ComputerId, long PlayerId, bool Snapshot, CapturedFrame Frame)> outgoing = new();
        lock (_lock) {
            foreach (var (computerId, cast) in _casts) {
                if (cast.TicksUntilSend > 0) {
                    cast.TicksUntilSend--;
                }
                if (cast.TicksUntilSend > 0) {
                    continue;
                }

                var snapshotFrame = new CapturedFrame(cast.CachedCommands, cast.CachedBackground, cast.CachedForeground);
                var sentAnything = false;

                foreach (var playerId in cast.Viewers) {
                    if (cast.NeedSnapshot.Contains(playerId)) {
                        outgoing.Add((computerId, playerId, true, snapshotFrame));
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

        foreach (var (computerId, playerId, snapshot, frame) in outgoing) {
            try {
                _channel.SendFrame(playerId, computerId, snapshot, FrameCodec.Encode(frame.Commands, frame.Background, frame.Foreground));
            } catch (Exception exception) {
                _log($"Frame send to player {playerId} failed. {exception.Message}");
            }
        }
    }

    private void RebuildWatched() {
        _watchedComputers = _casts.Where(pair => pair.Value.Viewers.Count > 0).Select(pair => pair.Key).ToHashSet();
    }
}
