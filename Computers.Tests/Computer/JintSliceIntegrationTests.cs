using Jint;
using Jint.Runtime;
using Xunit;

namespace Computers.Tests.Computer;

/// <summary>
/// Verifies the Jint mechanics the cooperative scheduler relies on
/// (see Docs/superpowers/specs/2026-08-29-cooperative-scheduler-design.md).
/// Decompile-verified: Jint.Runtime.EventLoop stores pending jobs in a
/// ConcurrentQueue, so completing an awaited Task from another thread is safe.
/// </summary>
public class JintSliceIntegrationTests {
    [Fact]
    public void AwaitedTaskCompletedFromAnotherThreadResumesOnProcessTasks() {
        var engine = new Engine(options => {
            options.ExperimentalFeatures = ExperimentalFeature.All;
        });

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resumed = false;
        engine.SetValue("gate", new Func<Task>(() => tcs.Task));
        engine.SetValue("done", new Action(() => resumed = true));

        engine.Execute("(async () => { await gate(); done(); })()");
        engine.Advanced.ProcessTasks();
        Assert.False(resumed); // still awaiting

        Task.Run(() => tcs.SetResult()).Wait();
        Thread.Sleep(50); // let Jint observe the completion

        engine.Advanced.ProcessTasks(); // slice on this thread
        Assert.True(resumed);
    }

    private record ProbeResponse(int StatusCode, string Body);

    [Fact]
    public void AwaitedValueTaskResolvesToTheResultValueDirectly() {
        // Documents why the JS HTTP wrappers must NOT destructure `{ Result }`:
        // modern Jint task interop resolves an awaited ValueTask<T> to T itself.
        var engine = new Engine(options => {
            options.ExperimentalFeatures = ExperimentalFeature.All;
        });

        engine.SetValue("request", new Func<ValueTask<ProbeResponse>>(
            () => new ValueTask<ProbeResponse>(new ProbeResponse(200, "ok"))));

        var status = 0;
        var hasResultProperty = true;
        engine.SetValue("report", new Action<int, bool>((s, r) => {
            status = s;
            hasResultProperty = r;
        }));

        engine.Execute("(async () => { const response = await request(); report(response.StatusCode, response.Result !== undefined); })()");
        engine.Advanced.ProcessTasks();
        Thread.Sleep(50);
        engine.Advanced.ProcessTasks();

        Assert.Equal(200, status);
        Assert.False(hasResultProperty); // no `.Result` wrapper - the value IS the response
    }

    [Fact]
    public void MaxStatementsAbortsSyncBusyLoop() {
        var engine = new Engine(options => {
            options.MaxStatements(10_000);
        });

        Assert.Throws<StatementsCountOverflowException>(
            () => engine.Execute("while (true) {}"));
    }

    [Fact]
    public void StatementBudgetResetsPerExecution() {
        var engine = new Engine(options => {
            options.MaxStatements(10_000);
        });

        // Many small executions must NOT accumulate toward the budget.
        for (var i = 0; i < 50; i++) {
            engine.Execute("for (let i = 0; i < 100; i++) {}");
        }
    }
}
