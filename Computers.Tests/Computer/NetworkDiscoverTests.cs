using Computers.Computer.Domain;
using Computers.Game;
using Jint;
using Jint.Runtime;
using Newtonsoft.Json.Linq;
using Computers.Tests.TestDoubles;
using Xunit;

namespace Computers.Tests.Computer;

/// <summary>
/// Drives the real /Core/Network module. Discover broadcasts to "*" and collects router
/// replies over frames; ConfigureRouter is an acked RPC to the router's own address.
/// </summary>
public class NetworkDiscoverTests {
    private class FakeNetwork {
        public List<(string Address, string Payload)> Sent { get; } = new();
        public void SendMessage(string address, string payload) => Sent.Add((address, payload));
        public string GetAddress() => "self";
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
            Engine.SetValue("__net", Engine.Modules.Import("/Core/Network"));
            Engine.SetValue("__events", Engine.Modules.Import("/Core/Events"));
        }

        // Mirrors the OS loop + scheduler: pump events to listeners, open the frame gate,
        // let the completion propagate, run a slice.
        public void Pump(int times = 3) {
            for (var i = 0; i < times; i++) {
                Engine.Execute("[...Event.Poll()].forEach(e => __events.DispatchToListeners(e))");
                System.OpenGate();
                Thread.Sleep(10);
                Engine.Advanced.ProcessTasks();
            }
        }
    }

    private const string DiscoverDriver =
        "(async () => { try { __ok(JSON.stringify(await __net.Discover({ collectFrames: 3 }))) } catch (e) { __err(String(e && e.message || e)) } })()";

    [Fact]
    public void DiscoverBroadcastsADiscoverRequest() {
        var rig = new Rig();
        rig.Engine.Execute(DiscoverDriver);

        var (address, payload) = Assert.Single(rig.Network.Sent);
        Assert.Equal("*", address);
        var request = JObject.Parse(payload);
        Assert.Equal("discover", request["cmd"]!.Value<string>());
        Assert.NotNull(request["cid"]);
    }

    [Fact]
    public void CollectsAcrossMultipleRouterRepliesDedupsExcludesSelfAndSorts() {
        var rig = new Rig();
        rig.Engine.Execute(DiscoverDriver);
        var cid = JObject.Parse(rig.Network.Sent[0].Payload)["cid"]!.Value<string>();

        rig.Events.Push("r1", "{\"re\":\"" + cid + "\",\"ok\":true,\"data\":{\"router\":\"r1\",\"endpoints\":[\"zz99\",\"aa11\",\"self\"]}}");
        rig.Pump(times: 1);
        rig.Events.Push("r2", "{\"re\":\"" + cid + "\",\"ok\":true,\"data\":{\"router\":\"r2\",\"endpoints\":[\"aa11\",\"mm55\"]}}");
        rig.Pump(times: 5);

        Assert.Null(rig.Error);
        Assert.Equal("[\"aa11\",\"mm55\",\"zz99\"]", rig.Ok);
    }

    [Fact]
    public void ResolvesToEmptyArrayWhenNothingReplies() {
        var rig = new Rig();
        rig.Engine.Execute(DiscoverDriver);
        rig.Pump(times: 5);

        Assert.Null(rig.Error);
        Assert.Equal("[]", rig.Ok);
    }

    [Fact]
    public void ConfigureRouterResolvesOnAck() {
        var rig = new Rig();
        rig.Engine.Execute(
            "(async () => { try { await __net.ConfigureRouter('r1', 7, { timeoutFrames: 5 }); __ok('acked') } catch (e) { __err(String(e && e.message || e)) } })()");

        var (address, payload) = Assert.Single(rig.Network.Sent);
        Assert.Equal("r1", address);
        var request = JObject.Parse(payload);
        Assert.Equal("configure", request["cmd"]!.Value<string>());
        Assert.Equal(7, request["channel"]!.Value<int>());

        var cid = request["cid"]!.Value<string>();
        rig.Events.Push("r1", "{\"re\":\"" + cid + "\",\"ok\":true,\"data\":null}");
        rig.Pump();
        Assert.Equal("acked", rig.Ok);
    }

    [Fact]
    public void ConfigureRouterRejectsOnTimeout() {
        var rig = new Rig();
        rig.Engine.Execute(
            "(async () => { try { await __net.ConfigureRouter('r1', null, { timeoutFrames: 2 }); __ok('acked') } catch (e) { __err(String(e && e.message || e)) } })()");
        rig.Pump(times: 5);

        Assert.Null(rig.Ok);
        Assert.Contains("timed out", rig.Error);
    }
}
