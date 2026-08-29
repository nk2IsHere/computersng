using System.Collections.Concurrent;
using Computers.Core;
using StardewModdingAPI;

namespace Computers.Computer.Domain;

public interface ISliceTarget {
    Id Id { get; }
    void OpenFrameGate();
    void RunSlice();
    void OnSliceError(Exception exception);
}

public class ComputerScheduler {
    private class Registration {
        public Registration(ISliceTarget target) {
            Target = target;
        }

        public ISliceTarget Target { get; }
        public int Scheduled; // 0 = idle, 1 = queued or running
    }

    private readonly IMonitor _monitor;
    private readonly int _gateDivisor;
    private readonly ConcurrentDictionary<Id, Registration> _targets = new();
    private readonly BlockingCollection<Registration> _jobs = new();

    private long _pulseCount;

    public ComputerScheduler(IMonitor monitor, Configuration configuration) {
        _monitor = monitor;

        var maxFps = Math.Clamp(configuration.Render.MaxFps, 1, 60);
        _gateDivisor = Math.Max(1, 60 / maxFps);

        var workers = configuration.Engine.SchedulerWorkers;
        if (workers <= 0) {
            workers = Math.Min(4, Math.Max(1, Environment.ProcessorCount / 2));
        }

        for (var i = 0; i < workers; i++) {
            var thread = new Thread(WorkerBody) {
                IsBackground = true,
                Name = $"ComputerScheduler-{i}"
            };
            thread.Start();
        }
    }

    public void Register(ISliceTarget target) {
        _targets.TryAdd(target.Id, new Registration(target));
    }

    public void Unregister(Id id) {
        _targets.TryRemove(id, out _);
    }

    public void Pulse() {
        var pulse = ++_pulseCount;
        var openGates = pulse % _gateDivisor == 0;

        foreach (var registration in _targets.Values) {
            if (openGates) {
                registration.Target.OpenFrameGate();
            }

            if (Interlocked.CompareExchange(ref registration.Scheduled, 1, 0) == 0) {
                _jobs.Add(registration);
            }
        }
    }

    private void WorkerBody() {
        foreach (var registration in _jobs.GetConsumingEnumerable()) {
            try {
                // Skip if unregistered after enqueue.
                if (_targets.ContainsKey(registration.Target.Id)) {
                    registration.Target.RunSlice();
                }
            }
            catch (Exception exception) {
                try {
                    registration.Target.OnSliceError(exception);
                }
                catch (Exception handlerException) {
                    _monitor.Log($"Slice error handler for {registration.Target.Id} threw: {handlerException}", LogLevel.Error);
                }
            }
            finally {
                Interlocked.Exchange(ref registration.Scheduled, 0);
            }
        }
    }
}
