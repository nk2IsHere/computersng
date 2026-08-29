using Computers.Computer;
using Computers.Computer.Domain;
using Computers.Core;
using Computers.Tests.TestDoubles;
using Xunit;

namespace Computers.Tests.Computer;

public class ComputerSchedulerTests {
    private class FakeTarget : ISliceTarget {
        private readonly Action? _onSlice;

        public FakeTarget(Id id, Action? onSlice = null) {
            Id = id;
            _onSlice = onSlice;
        }

        public Id Id { get; }
        public int GateOpens;
        public int CompletedSlices;
        public int Errors;
        public int ConcurrentSlices;
        public int MaxConcurrentSlices;

        public void OpenFrameGate() => Interlocked.Increment(ref GateOpens);

        public void RunSlice() {
            var now = Interlocked.Increment(ref ConcurrentSlices);
            InterlockedMax(ref MaxConcurrentSlices, now);
            try {
                _onSlice?.Invoke();
            } finally {
                Interlocked.Decrement(ref ConcurrentSlices);
                Interlocked.Increment(ref CompletedSlices);
            }
        }

        public void OnSliceError(Exception exception) => Interlocked.Increment(ref Errors);

        private static void InterlockedMax(ref int location, int value) {
            int current;
            while ((current = Volatile.Read(ref location)) < value
                   && Interlocked.CompareExchange(ref location, value, current) != current) {
            }
        }
    }

    private static Configuration TestConfiguration(int workers = 2, int maxFps = 60) => new() {
        Render = new RenderConfiguration { MaxFps = maxFps },
        Engine = new EngineConfiguration { SchedulerWorkers = workers }
    };

    private static void WaitUntil(Func<bool> condition, int timeoutMs = 2000) {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline) {
            Thread.Sleep(5);
        }
        Assert.True(condition(), "condition not reached within timeout");
    }

    [Fact]
    public void PulseOpensGateAndRunsSlice() {
        var scheduler = new ComputerScheduler(new TestMonitor(), TestConfiguration());
        var target = new FakeTarget("c.one".AsId());
        scheduler.Register(target);

        scheduler.Pulse();

        WaitUntil(() => Volatile.Read(ref target.CompletedSlices) == 1);
        Assert.Equal(1, Volatile.Read(ref target.GateOpens));
    }

    [Fact]
    public void SlowSliceIsNotDoubleEnqueued() {
        using var release = new ManualResetEventSlim(false);
        var scheduler = new ComputerScheduler(new TestMonitor(), TestConfiguration());
        var target = new FakeTarget("c.slow".AsId(), onSlice: () => release.Wait());
        scheduler.Register(target);

        scheduler.Pulse();
        WaitUntil(() => Volatile.Read(ref target.ConcurrentSlices) == 1);
        scheduler.Pulse();
        scheduler.Pulse();
        release.Set();
        WaitUntil(() => Volatile.Read(ref target.ConcurrentSlices) == 0);

        Assert.Equal(1, Volatile.Read(ref target.MaxConcurrentSlices)); // never concurrent
    }

    [Fact]
    public void SliceExceptionRoutesToOnSliceErrorAndWorkerSurvives() {
        var scheduler = new ComputerScheduler(new TestMonitor(), TestConfiguration(workers: 1));
        var failing = new FakeTarget("c.bad".AsId(), onSlice: () => throw new InvalidOperationException("boom"));
        scheduler.Register(failing);

        scheduler.Pulse();
        WaitUntil(() => Volatile.Read(ref failing.Errors) == 1);

        scheduler.Pulse(); // the single worker must still be alive to run this
        WaitUntil(() => Volatile.Read(ref failing.Errors) == 2);
    }

    [Fact]
    public void UnregisterStopsFutureSlices() {
        var scheduler = new ComputerScheduler(new TestMonitor(), TestConfiguration());
        var target = new FakeTarget("c.gone".AsId());
        scheduler.Register(target);
        scheduler.Pulse();
        WaitUntil(() => Volatile.Read(ref target.CompletedSlices) == 1);

        scheduler.Unregister(target.Id);
        scheduler.Pulse();
        Thread.Sleep(50);
        Assert.Equal(1, Volatile.Read(ref target.GateOpens));
        Assert.Equal(1, Volatile.Read(ref target.CompletedSlices));
    }

    [Fact]
    public void GateOpensOnDivisorTicksOnly() {
        // maxFps 30 -> divisor 2 -> gate on every second pulse; slices every pulse.
        var scheduler = new ComputerScheduler(new TestMonitor(), TestConfiguration(maxFps: 30));
        var target = new FakeTarget("c.half".AsId());
        scheduler.Register(target);

        for (var i = 0; i < 4; i++) {
            scheduler.Pulse();
        }
        WaitUntil(() => Volatile.Read(ref target.GateOpens) == 2);
        Assert.Equal(2, Volatile.Read(ref target.GateOpens));
    }

    [Fact]
    public void RegisterIsIdempotentPerId() {
        var scheduler = new ComputerScheduler(new TestMonitor(), TestConfiguration());
        var target = new FakeTarget("c.dup".AsId());
        scheduler.Register(target);
        scheduler.Register(target);
        scheduler.Pulse();
        WaitUntil(() => Volatile.Read(ref target.CompletedSlices) == 1);
        Assert.Equal(1, Volatile.Read(ref target.GateOpens));
    }
}
