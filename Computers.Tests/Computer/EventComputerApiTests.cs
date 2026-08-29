using Computers.Computer;
using Computers.Computer.Domain.Api;
using Computers.Core;
using Computers.Tests.TestDoubles;
using Xunit;

namespace Computers.Tests.Computer;

public class EventComputerApiTests {
    private static readonly Id C1 = "computer.c1".AsId();

    [Fact]
    public void NetworkMessageIsPollableExactlyOnce() {
        var api = new EventComputerApi(new FakeComputerPort(C1));
        var messageId = Guid.NewGuid();

        api.ReceiveEvent(new NetworkMessageComputerEvent(C1, messageId, "src", "payload"));
        api.ReceiveEvent(new NetworkMessageComputerEvent(C1, messageId, "src", "payload")); // duplicate

        var state = (EventComputerState) api.Api;
        var events = state.Poll();
        var entry = Assert.Single(events, e => e.Type == "NetworkMessage");
        Assert.Equal(new object[] { "src", "payload" }, entry.Data);
    }

    [Fact]
    public void DistinctMessagesBothArrive() {
        var api = new EventComputerApi(new FakeComputerPort(C1));
        api.ReceiveEvent(new NetworkMessageComputerEvent(C1, Guid.NewGuid(), "a", "1"));
        api.ReceiveEvent(new NetworkMessageComputerEvent(C1, Guid.NewGuid(), "b", "2"));

        var state = (EventComputerState) api.Api;
        Assert.Equal(2, state.Poll().Count(e => e.Type == "NetworkMessage"));
    }

    [Fact]
    public void EmptyPollAllocatesNothing() {
        var api = new EventComputerApi(new FakeComputerPort(C1));
        var state = (EventComputerState) api.Api;

        var first = state.Poll();
        var second = state.Poll();

        Assert.Empty(first);
        Assert.Same(first, second); // shared zero-allocation empty array
    }

    [Fact]
    public void ResetClearsDedupCacheAndQueue() {
        var api = new EventComputerApi(new FakeComputerPort(C1));
        var messageId = Guid.NewGuid();
        api.ReceiveEvent(new NetworkMessageComputerEvent(C1, messageId, "src", "p"));
        api.Reset();
        api.ReceiveEvent(new NetworkMessageComputerEvent(C1, messageId, "src", "p"));

        var state = (EventComputerState) api.Api;
        Assert.Single(state.Poll(), e => e.Type == "NetworkMessage");
    }
}
