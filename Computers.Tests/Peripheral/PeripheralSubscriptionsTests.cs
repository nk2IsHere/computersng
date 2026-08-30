using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.Peripheral;

public class PeripheralSubscriptionsTests {
    private static PeripheralSubscriptions Make(int maxSubscribers = 2) =>
        new(new[] { "ready", "day" }, maxSubscribers);

    private static JObject Events(string json) => JObject.Parse(json);

    [Fact]
    public void NonSubscriptionCommandsAreNotHandled() {
        Assert.False(Make().TryHandle("list", Events("{}"), "c1"));
        Assert.False(Make().TryHandle(null, Events("{}"), "c1"));
    }

    [Fact]
    public void SubscribeAndUnsubscribeManageSubscribers() {
        var subscriptions = Make();

        Assert.True(subscriptions.TryHandle("subscribe", Events("{\"events\":[\"ready\"]}"), "c1"));
        Assert.Contains("c1", subscriptions.Subscribers("ready"));
        Assert.Empty(subscriptions.Subscribers("day"));

        Assert.True(subscriptions.TryHandle("unsubscribe", Events("{\"events\":[\"ready\"]}"), "c1"));
        Assert.Empty(subscriptions.Subscribers("ready"));
    }

    [Fact]
    public void DuplicateSubscribeIsIdempotentAndLimitIsEnforced() {
        var subscriptions = Make(maxSubscribers: 2);

        subscriptions.TryHandle("subscribe", Events("{\"events\":[\"ready\"]}"), "c1");
        subscriptions.TryHandle("subscribe", Events("{\"events\":[\"ready\"]}"), "c1");
        Assert.Single(subscriptions.Subscribers("ready"));

        subscriptions.TryHandle("subscribe", Events("{\"events\":[\"ready\"]}"), "c2");
        var exception = Assert.Throws<PeripheralRequestException>(
            () => subscriptions.TryHandle("subscribe", Events("{\"events\":[\"ready\"]}"), "c3"));
        Assert.Equal("subscriber limit reached", exception.Message);
    }

    [Fact]
    public void UnsupportedEventThrows() {
        var exception = Assert.Throws<PeripheralRequestException>(
            () => Make().TryHandle("subscribe", Events("{\"events\":[\"ready\",\"nope\"]}"), "c1"));
        Assert.Equal("unsupported event 'nope'", exception.Message);
        Assert.Empty(Make().Subscribers("ready"));
    }

    [Fact]
    public void MissingOrEmptyEventsListThrows() {
        Assert.Throws<PeripheralRequestException>(() => Make().TryHandle("subscribe", Events("{}"), "c1"));
        Assert.Throws<PeripheralRequestException>(() => Make().TryHandle("subscribe", Events("{\"events\":[]}"), "c1"));
    }
}
