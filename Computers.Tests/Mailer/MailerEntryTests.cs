using Computers.Computer;
using Computers.Core;
using Computers.Mailer;
using Computers.Mailer.Domain;
using Computers.Peripheral;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Tests.TestDoubles;
using Newtonsoft.Json.Linq;
using Xunit;
using Context = Computers.Core.Context;

namespace Computers.Tests.Mailer;

public class MailerEntryTests {
    private static readonly Id FactoryId = "factory.mailer".AsId();
    private static readonly Id M1 = "mailer.m1".AsId();
    private static readonly Id R1 = "router.r1".AsId();

    private class FakeMailerWorld : IMailerWorld {
        public List<(string Text, bool Error)> Notifications { get; } = new();
        public List<string> QueuedMail { get; } = new();
        public int Invalidations { get; private set; }

        public void Notify(string text, bool error) => Notifications.Add((text, error));
        public void QueueMail(string mailId) => QueuedMail.Add(mailId);
        public void InvalidateMailData() => Invalidations++;
    }

    private static Configuration TestConfiguration() => new() {
        Network = new NetworkConfiguration {
            BlockedAddresses = new List<string>(),
            AllowedAddresses = new List<string>(),
            MessageTtl = 16
        }
    };

    private static (MailerStatefulDataContextEntry Mailer, FakeMailerWorld World, FakeRouterPort Router) Make() {
        var configuration = TestConfiguration();
        var routerPort = new FakeRouterPort(R1);
        var routers = new ContextLookup<IRouterPort>(
            () => new HashSet<ContextEntry<IRouterPort>> { new(R1, routerPort) });
        var registry = new NetworkRegistry(new TestMonitor(), configuration, routers);
        var world = new FakeMailerWorld();

        registry.Register(new Placement(R1, NodeRole.Router, "Farm", 0, 0));
        registry.Register(new Placement(M1, NodeRole.Endpoint, "Farm", 5, 0));

        var mailer = new MailerStatefulDataContextEntry(
            FactoryId, M1, new TestMonitor(), configuration, registry, routers, world);
        mailer.Fire(new StartPeripheralEvent());
        return (mailer, world, routerPort);
    }

    private static void Send(MailerStatefulDataContextEntry mailer, string payload) {
        mailer.ReceiveDatagram(new Datagram(Guid.NewGuid(), "c1", "m1", 16, payload));
    }

    private static JObject LastPayload(FakeRouterPort router) =>
        JObject.Parse(router.Delivered[^1].Datagram.Payload);

    [Fact]
    public void NotifyReachesTheWorld() {
        var (mailer, world, router) = Make();
        Send(mailer, "{\"cid\":\"n1\",\"cmd\":\"notify\",\"text\":\"furnaces done\"}");
        mailer.Fire(new TickPeripheralEvent(1));

        Assert.True(LastPayload(router)["ok"]!.Value<bool>());
        Assert.Equal(("furnaces done", false), world.Notifications.Single());
    }

    [Fact]
    public void MailStoresTheLetterQueuesItAndInvalidates() {
        var (mailer, world, router) = Make();
        Send(mailer, "{\"cid\":\"l1\",\"cmd\":\"mail\",\"text\":\"hello\",\"title\":\"Report\"}");
        mailer.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.True(reply["ok"]!.Value<bool>());
        var mailId = reply["data"]!["mailId"]!.Value<string>()!;
        Assert.Equal(new[] { mailId }, world.QueuedMail);
        Assert.Equal(1, world.Invalidations);
        Assert.Equal(new Letter(mailId, "hello", "Report"), mailer.Letters.Single());
    }

    [Fact]
    public void LettersSurviveStoreRestore() {
        var (mailer, world, _) = Make();
        Send(mailer, "{\"cid\":\"l2\",\"cmd\":\"mail\",\"text\":\"persist me\"}");
        mailer.Fire(new TickPeripheralEvent(1));
        var mailId = world.QueuedMail.Single();

        var state = mailer.Store(Context.Empty);
        var (fresh, _, _) = Make();
        fresh.Restore(Context.Empty, state);

        Assert.Equal(mailer.Letters.Single(mailIdMatch => mailIdMatch.MailId == mailId), fresh.Letters.Single());
    }

    [Fact]
    public void MissingTextIsAnError() {
        var (mailer, _, router) = Make();
        Send(mailer, "{\"cid\":\"e1\",\"cmd\":\"notify\"}");
        mailer.Fire(new TickPeripheralEvent(1));

        var reply = LastPayload(router);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Equal("text is required", reply["error"]!.Value<string>());
    }
}
