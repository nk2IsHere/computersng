using Computers.Router.Domain;
using Xunit;

namespace Computers.Tests.Router;

public class SeenMessageCacheTests {
    [Fact]
    public void RemembersNewIdsOnce() {
        var cache = new SeenMessageCache(10);
        var id = Guid.NewGuid();
        Assert.True(cache.Remember(id));
        Assert.False(cache.Remember(id));
    }

    [Fact]
    public void EvictsOldestBeyondCapacity() {
        var cache = new SeenMessageCache(2);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var third = Guid.NewGuid();
        cache.Remember(first);
        cache.Remember(second);
        cache.Remember(third); // evicts first
        Assert.True(cache.Remember(first));  // first is new again
        Assert.False(cache.Remember(third)); // third still known
    }

    [Fact]
    public void ClearForgetsEverything() {
        var cache = new SeenMessageCache(10);
        var id = Guid.NewGuid();
        cache.Remember(id);
        cache.Clear();
        Assert.True(cache.Remember(id));
    }
}
