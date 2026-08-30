using Computers.Computer.Domain;
using Computers.Game;
using Computers.Tests.TestDoubles;
using Jint;
using Jint.Runtime;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.Computer;

/// <summary>
/// Drives the real /Core/Peripheral module (imported from assets/Library via the module
/// loader) in a bare Jint engine with fake Network/Event/System globals, covering the RPC
/// loop: request shape, correlation matching, error replies, timeout, event forwarding.
/// </summary>
public class PeripheralRpcTests {
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
            Engine.SetValue("__peripheral", Engine.Modules.Import("/Core/Peripheral"));
            Engine.SetValue("__events", Engine.Modules.Import("/Core/Events"));
            Engine.SetValue("__unclaimed", new Action<string>(value => Unclaimed.Add(value)));
        }

        public List<string> Unclaimed { get; } = new();

        // Mirrors the OS loop + scheduler: pump events to listeners, open the frame gate,
        // let the completion propagate, run a slice.
        public void Pump(int times = 3) {
            for (var i = 0; i < times; i++) {
                Engine.Execute("__unclaimed(JSON.stringify([...Event.Poll()].filter(e => __events.DispatchToListeners(e) !== true).map(e => e.Type)))");
                System.OpenGate();
                Thread.Sleep(10);
                Engine.Advanced.ProcessTasks();
            }
        }
    }

    private const string CallDriver =
        "(async () => { try { __ok(JSON.stringify(await __peripheral.Call('ab12', 'list', { extra: 1 }, { timeoutFrames: 5 }))) } catch (e) { __err(String(e && e.message || e)) } })()";

    [Fact]
    public void RequestCarriesCidCmdArgsToTheAddress() {
        var rig = new Rig();
        rig.Engine.Execute(CallDriver);

        var (address, payload) = Assert.Single(rig.Network.Sent);
        Assert.Equal("ab12", address);
        var request = JObject.Parse(payload);
        Assert.Equal("c1", request["cid"]!.Value<string>());
        Assert.Equal("list", request["cmd"]!.Value<string>());
        Assert.Equal(1, request["extra"]!.Value<int>());
    }

    [Fact]
    public void MatchingReplyResolvesWithData() {
        var rig = new Rig();
        rig.Engine.Execute(CallDriver);

        rig.Events.Push("ab12", "{\"re\":\"c1\",\"ok\":true,\"data\":{\"machines\":[]}}");
        rig.Pump();

        Assert.Null(rig.Error);
        Assert.NotNull(rig.Ok);
        Assert.Contains("machines", rig.Ok);
    }

    [Fact]
    public void ErrorReplyRejectsWithTheErrorText() {
        var rig = new Rig();
        rig.Engine.Execute(CallDriver);

        rig.Events.Push("ab12", "{\"re\":\"c1\",\"ok\":false,\"error\":\"machine not in group\"}");
        rig.Pump();

        Assert.Null(rig.Ok);
        Assert.Equal("machine not in group", rig.Error);
    }

    [Fact]
    public void NoReplyTimesOut() {
        var rig = new Rig();
        rig.Engine.Execute(CallDriver);
        rig.Pump(times: 8); // more gates than timeoutFrames: 5

        Assert.Null(rig.Ok);
        Assert.NotNull(rig.Error);
        Assert.Contains("timed out", rig.Error);
    }

    [Fact]
    public void ReplyIsClaimedWhileUnrelatedEventsFlowBackToTheLoop() {
        var rig = new Rig();
        rig.Engine.Execute(
            "(async () => { try { __ok(JSON.stringify(await __peripheral.Call('ab12', 'ping', {}, { timeoutFrames: 5 }))) } catch (e) { __err(String(e && e.message || e)) } })()");

        rig.Events.Push("zz99", "{\"re\":\"other\",\"ok\":true,\"data\":1}"); // not ours
        rig.Events.Push("ab12", "{\"re\":\"c1\",\"ok\":true,\"data\":\"pong\"}");
        rig.Pump();

        Assert.Null(rig.Error);
        Assert.Equal("\"pong\"", rig.Ok);

        // The pump saw exactly one unclaimed event (the unrelated one); the reply was claimed.
        var firstPump = rig.Unclaimed[0];
        Assert.Equal("[\"NetworkMessage\"]", firstPump);
    }
}
