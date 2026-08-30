using Computers.Core;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Tests.TestDoubles;
using Xunit;
using Context = Computers.Core.Context;

namespace Computers.Tests.Core;

public class ContextEntryStateTests {
    [Fact]
    public void EmptyIsNotPollutedByPreviousMutation() {
        // Regression: Empty was a shared singleton; Store() mutating it made every
        // subsequently produced entry reuse the last stored entry's id.
        var state = ContextEntryState.Empty;
        state.Id = "a.b.stale".AsId();

        Assert.Null(ContextEntryState.Empty.Id);
    }

    [Fact]
    public void StoringARouterDoesNotLeakItsIdIntoNewlyProducedEntries() {
        var configuration = new Computers.Computer.Configuration {
            Network = new Computers.Computer.NetworkConfiguration {
                BlockedAddresses = new List<string>(),
                AllowedAddresses = new List<string>()
            }
        };
        var routers = new ContextLookup<IRouterPort>(() => new HashSet<ContextEntry<IRouterPort>>());
        var endpoints = new ContextLookup<INetworkEndpoint>(
            () => new HashSet<ContextEntry<INetworkEndpoint>>());
        var registry = new NetworkRegistry(new TestMonitor(), configuration, routers);

        var factory = new RouterStatefulDataContextEntryFactory(
            "factory.router".AsId(), "router".AsId(),
            new TestMonitor(), configuration, registry, endpoints, routers);

        var first = (RouterStatefulDataContextEntry) factory.ProduceValue();
        first.Store(Context.Empty); // mutated the shared Empty before the fix

        var second = (RouterStatefulDataContextEntry) factory.ProduceValue();
        Assert.NotEqual(first.Id, second.Id);
    }
}
