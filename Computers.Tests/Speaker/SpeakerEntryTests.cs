using Computers.Computer;
using Computers.Core;
using Computers.Peripheral;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Speaker;
using Computers.Speaker.Domain;
using Computers.Tests.TestDoubles;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.Speaker;

public class SpeakerEntryTests {
    private static readonly Id FactoryId = "factory.speaker".AsId();
    private static readonly Id S1 = "speaker.s1".AsId();
    private static readonly Id R1 = "router.r1".AsId();

    private class FakeSpeakerWorld : ISpeakerWorld {
        public List<(string Cue, int? Pitch)> Played { get; } = new();

        public bool Play(Placement placement, string cue, int? pitch) {
            if (cue == "nope") {
                return false;
            }
            Played.Add((cue, pitch));
            return true;
        }
    }

    private static Configuration TestConfiguration() => new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>(),
            MessageTtl = 16
        }
    };

    private static (SpeakerStatefulDataContextEntry Speaker, FakeSpeakerWorld World, FakeRouterPort Router) Make() {
        var configuration = TestConfiguration();
        var routerPort = new FakeRouterPort(R1);
        var routers = new ContextLookup<IRouterPort>(
            () => new HashSet<ContextEntry<IRouterPort>> { new(R1, routerPort) });
        var registry = new NetworkRegistry(new TestMonitor(), configuration, routers);
        var world = new FakeSpeakerWorld();

        registry.Register(new Placement(R1, NodeRole.Router, "Farm", 0, 0));
        registry.Register(new Placement(S1, NodeRole.Endpoint, "Farm", 5, 0));

        var speaker = new SpeakerStatefulDataContextEntry(
            FactoryId, S1, new TestMonitor(), configuration, registry, routers, world);
        speaker.Fire(new StartPeripheralEvent());
        return (speaker, world, routerPort);
    }

    private static void Send(SpeakerStatefulDataContextEntry speaker, string payload) {
        speaker.ReceiveDatagram(new Datagram(Guid.NewGuid(), "c1", "s1", 16, payload));
    }

    private static JObject LastPayload(FakeRouterPort router) =>
        JObject.Parse(router.Delivered[^1].Datagram.Payload);

    [Fact]
    public void PlayReachesTheWorldWithPitch() {
        var (speaker, world, router) = Make();
        Send(speaker, "{\"cid\":\"p1\",\"cmd\":\"play\",\"cue\":\"furnace\",\"pitch\":1200}");
        speaker.Fire(new TickPeripheralEvent(1));

        Assert.True(LastPayload(router)["ok"]!.Value<bool>());
        Assert.Equal(("furnace", (int?)1200), world.Played.Single());
    }

    [Fact]
    public void UnknownCueIsAnError() {
        var (speaker, world, router) = Make();
        Send(speaker, "{\"cid\":\"p2\",\"cmd\":\"play\",\"cue\":\"nope\"}");
        speaker.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Contains("unknown cue", reply["error"]!.Value<string>());
        Assert.Empty(world.Played);
    }

    [Fact]
    public void PingRepliesWithType() {
        var (speaker, _, router) = Make();
        Send(speaker, "{\"cid\":\"p3\",\"cmd\":\"ping\"}");
        speaker.Fire(new TickPeripheralEvent(1));

        Assert.Equal("speaker", LastPayload(router)["data"]!["type"]!.Value<string>());
    }
}
