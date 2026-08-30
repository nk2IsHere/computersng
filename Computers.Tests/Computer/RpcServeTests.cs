using Computers.Computer.Domain;
using Computers.Game;
using Jint;
using Jint.Runtime;
using Newtonsoft.Json.Linq;
using Computers.Tests.TestDoubles;
using Xunit;

namespace Computers.Tests.Computer;

/// <summary>
/// Drives the real /View/RpcServerView and /Core/Rpc modules in a bare Jint engine with
/// fake Network/Event/System globals. The view answers served requests, ignores replies
/// and event pushes, and Call still works from /Core/Rpc.
/// </summary>
public class RpcServeTests {
    private class FakeNetwork {
        public List<(string Address, string Payload)> Sent { get; } = new();
        public void SendMessage(string address, string payload) => Sent.Add((address, payload));
    }

    private class FakeEvents {
        private readonly List<object> _queue = new();
        public void Push(string sourceAddress, string payload) {
            _queue.Add(new Dictionary<string, object> {
                ["Type"] = "NetworkMessage",
                ["Data"] = new object[] { sourceAddress, payload }
            });
        }
        public object[] Poll() {
            var drained = _queue.ToArray();
            _queue.Clear();
            return drained;
        }
    }

    private class FakeSystem {
        private TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task NextFrame() => _gate.Task;

        public void OpenGate() {
            var previous = Interlocked.Exchange(
                ref _gate, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            previous.TrySetResult();
        }
    }

    private sealed class Rig {
        public Engine Engine { get; }
        public FakeNetwork Network { get; } = new();
        public FakeEvents Events { get; } = new();
        public FakeSystem System { get; } = new();
        public string? Ok { get; private set; }
        public string? Error { get; private set; }

        public Rig() {
            var loader = new FileSystemLibraryLoader();
            Engine = new Engine(options => {
                options.Strict();
                options.EnableModules(new ComputerModuleLoader(new TestMonitor(), new List<IRedundantLoader> { loader }));
                options.ExperimentalFeatures = ExperimentalFeature.All;
                options.CatchClrExceptions();
            });

            Engine.SetValue("Network", Network);
            Engine.SetValue("Event", Events);
            Engine.SetValue("System", System);
            Engine.SetValue("__ok", new Action<string>(value => Ok = value));
            Engine.SetValue("__err", new Action<string>(value => Error = value));
            Engine.SetValue("__rpc", Engine.Modules.Import("/Core/Rpc"));
            Engine.SetValue("__events", Engine.Modules.Import("/Core/Events"));
            Engine.SetValue("__rpcView", Engine.Modules.Import("/View/RpcServerView"));
            Engine.SetValue("__fire", Engine.Modules.Import("/View/FireResult"));
            Engine.SetValue("__unclaimed", new Action<string>(value => Unclaimed.Add(value)));
            Engine.Execute("globalThis.__serverView = new __rpcView.RpcServerView()");
        }

        public List<string> Unclaimed { get; } = new();

        // Mirrors the OS loop + scheduler: pump events to listeners, open the frame gate,
        // let the completion propagate, run a slice.
        public void Pump(int times = 3) {
            for (var i = 0; i < times; i++) {
                Engine.Execute(
                    "__unclaimed(JSON.stringify([...Event.Poll()]" +
                    ".filter(e => __events.DispatchToListeners(e) !== true)" +
                    ".filter(e => __serverView.Fire(e) !== __fire.FireResult.Claimed)" +
                    ".map(e => e.Type)))");
                System.OpenGate();
                Thread.Sleep(10);
                Engine.Advanced.ProcessTasks();
            }
        }
    }

    [Fact]
    public void RegisteredHandlerAnswersARequestWithItsData() {
        var rig = new Rig();
        rig.Engine.Execute("__serverView.handlers.set('echo', (args) => ({ doubled: args.value * 2 }))");
        rig.Events.Push("ab12", "{\"cid\":\"k1\",\"cmd\":\"echo\",\"value\":21}");
        rig.Pump();

        var (address, payload) = Assert.Single(rig.Network.Sent);
        Assert.Equal("ab12", address);
        var reply = JObject.Parse(payload);
        Assert.Equal("k1", reply["re"]!.Value<string>());
        Assert.True(reply["ok"]!.Value<bool>());
        Assert.Equal(42, reply["data"]!["doubled"]!.Value<int>());
    }

    [Fact]
    public void UnknownCommandGetsErrorReply() {
        var rig = new Rig();
        rig.Engine.Execute("__serverView.handlers.set('echo', (args) => args)");
        rig.Events.Push("ab12", "{\"cid\":\"k2\",\"cmd\":\"reboot\"}");
        rig.Pump();

        var (_, payload) = Assert.Single(rig.Network.Sent);
        var reply = JObject.Parse(payload);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Equal("unknown command", reply["error"]!.Value<string>());
    }

    [Fact]
    public void ThrowingHandlerRepliesWithTheErrorText() {
        var rig = new Rig();
        rig.Engine.Execute("__serverView.handlers.set('boom', () => { throw new Error('kaput') })");
        rig.Events.Push("ab12", "{\"cid\":\"k3\",\"cmd\":\"boom\"}");
        rig.Pump();

        var (_, payload) = Assert.Single(rig.Network.Sent);
        var reply = JObject.Parse(payload);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Equal("kaput", reply["error"]!.Value<string>());
    }

    [Fact]
    public void RepliesAndEventPushesAreNotTreatedAsRequests() {
        var rig = new Rig();
        rig.Engine.Execute("__serverView.handlers.set('echo', (args) => args)");
        rig.Events.Push("ab12", "{\"cid\":\"c9\",\"cmd\":\"echo\",\"re\":\"other\"}"); // a reply, even with cid+cmd
        rig.Events.Push("ab12", "{\"event\":\"machineReady\"}");                        // an event push
        rig.Pump();

        Assert.Empty(rig.Network.Sent);
    }

    [Fact]
    public void RequestsWithoutRegisteredHandlersFlowBackToTheLoopUnclaimed() {
        var rig = new Rig(); // no handlers registered, so the view stays silent
        rig.Events.Push("ab12", "{\"cid\":\"k4\",\"cmd\":\"echo\"}");
        rig.Pump();

        Assert.Empty(rig.Network.Sent);
        Assert.Equal("[\"NetworkMessage\"]", rig.Unclaimed[0]);
    }

    [Fact]
    public void ConstructorHandlersAnswerImmediately() {
        var rig = new Rig();
        rig.Engine.Execute("globalThis.__serverView = new __rpcView.RpcServerView({ ping: () => ({ type: 'computer' }) })");
        rig.Events.Push("ab12", "{\"cid\":\"k5\",\"cmd\":\"ping\"}");
        rig.Pump();

        var (_, payload) = Assert.Single(rig.Network.Sent);
        var reply = JObject.Parse(payload);
        Assert.True(reply["ok"]!.Value<bool>());
        Assert.Equal("computer", reply["data"]!["type"]!.Value<string>());
    }

    [Fact]
    public void CallStillWorksFromTheRpcModule() {
        var rig = new Rig();
        rig.Engine.Execute(
            "(async () => { try { __ok(JSON.stringify(await __rpc.Call('ab12', 'ping', {}, { timeoutFrames: 5 }))) } catch (e) { __err(String(e && e.message || e)) } })()");
        rig.Events.Push("ab12", "{\"re\":\"c1\",\"ok\":true,\"data\":\"pong\"}");
        rig.Pump();

        Assert.Equal("\"pong\"", rig.Ok);
    }
}
