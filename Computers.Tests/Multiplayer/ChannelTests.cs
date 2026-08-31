using Computers.Multiplayer;
using Computers.Multiplayer.Domain;
using Computers.Multiplayer.Domain.Wire;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.Multiplayer;

public class ChannelTests {
    private static (HostChannel Host, FakeTransport HostSide, ClientChannel Client, FakeTransport ClientSide) Wire() {
        var hostSide = new FakeTransport { LocalPlayerId = 1, IsHost = true, HostPlayerId = 1 };
        var clientSide = new FakeTransport { LocalPlayerId = 2, IsHost = false, HostPlayerId = 1 };
        var host = new HostChannel(hostSide, _ => { });
        var client = new ClientChannel(clientSide, 10, _ => { });
        return (host, hostSide, client, clientSide);
    }

    [Fact]
    public void CallRoundTripsThroughBothChannels() {
        var (host, hostSide, client, clientSide) = Wire();
        host.Register("openScreen", (playerId, request) => {
            Assert.Equal(2, playerId);
            Assert.IsType<OpenScreenRequest>(request);
            return new OpenScreenResult("comp1", 452, 256);
        });

        JObject? reply = null;
        client.Call("openScreen", new { x = 1, y = 2, location = "Farm" }, data => reply = data, error => throw new Exception(error));
        var request = Assert.Single(clientSide.Sent);
        hostSide.Deliver(2, request.Message);
        host.Tick();
        var replyMessage = Assert.Single(hostSide.Sent);
        clientSide.Deliver(1, replyMessage.Message);
        client.Tick();

        Assert.Equal("comp1", reply!["computerId"]!.Value<string>());
        Assert.Equal(452, reply["canvasWidth"]!.Value<int>());
    }

    [Fact]
    public void HandlerErrorBecomesFailureReply() {
        var (host, hostSide, client, clientSide) = Wire();
        host.Register("openScreen", (_, _) => throw new ChannelRequestException("no computer at that tile"));
        string? error = null;
        client.Call("openScreen", new { x = 1, y = 2 }, _ => { }, e => error = e);
        hostSide.Deliver(2, clientSide.Sent[0].Message);
        host.Tick();
        clientSide.Deliver(1, hostSide.Sent[0].Message);
        client.Tick();
        Assert.Equal("no computer at that tile", error);
    }

    [Fact]
    public void VersionMismatchGetsReadableError() {
        var (host, hostSide, _, _) = Wire();
        host.Register("openScreen", (_, _) => null);
        var doctored = JObject.Parse("{\"cid\":\"c9\",\"cmd\":\"openScreen\",\"v\":999,\"x\":1,\"y\":1}");
        hostSide.Deliver(2, new ChannelMessage(doctored.ToString(Newtonsoft.Json.Formatting.None), null));
        host.Tick();
        var reply = JObject.Parse(hostSide.Sent[0].Message.Json!);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Contains("protocol version mismatch", reply["error"]!.Value<string>());
    }

    [Fact]
    public void CallTimesOutAfterConfiguredTicks() {
        var (_, _, client, _) = Wire();
        string? error = null;
        client.Call("openScreen", new { x = 1, y = 2 }, _ => { }, e => error = e);
        for (var i = 0; i < 11; i++) {
            client.Tick();
        }
        Assert.Contains("timed out", error);
    }

    [Fact]
    public void FramesReachTheFrameEvent() {
        var (host, hostSide, client, clientSide) = Wire();
        (string ComputerId, bool Snapshot, byte[] Body)? received = null;
        client.FrameReceived += (id, snapshot, body) => received = (id, snapshot, body);
        host.SendFrame(2, "comp1", true, new byte[] { 9, 9 });
        clientSide.Deliver(1, hostSide.Sent[0].Message);
        client.Tick();
        Assert.True(received!.Value.Snapshot);
        Assert.Equal(new byte[] { 9, 9 }, received.Value.Body);
    }

    [Fact]
    public void HostIgnoresRepliesAndEventsInsteadOfAnsweringThem() {
        var (host, hostSide, _, _) = Wire();
        host.Register("openScreen", (_, _) => null);
        hostSide.Deliver(1, new ChannelMessage("{\"re\":\"q1\",\"ok\":true,\"data\":null,\"error\":null}", null));
        hostSide.Deliver(1, new ChannelMessage("{\"event\":\"frame\",\"computerId\":\"a\",\"snapshot\":false}", new byte[] { 1 }));
        hostSide.Deliver(1, new ChannelMessage("{\"hello\":true}", null));
        host.Tick();
        Assert.Empty(hostSide.Sent);
    }

    [Fact]
    public void MessagesNotFromTheHostAreIgnored() {
        var (_, _, client, clientSide) = Wire();
        var seen = false;
        client.EventReceived += _ => seen = true;
        clientSide.Deliver(7, new ChannelMessage("{\"event\":\"screenClosed\",\"computerId\":\"a\"}", null));
        client.Tick();
        Assert.False(seen);
    }
}
