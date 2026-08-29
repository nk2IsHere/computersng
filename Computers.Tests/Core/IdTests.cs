using Computers.Core;
using Xunit;

namespace Computers.Tests.Core;

public class IdTests {
    [Fact]
    public void ParseRoundTripsThroughValue() {
        var id = Id.Parse("tools.kot.nk2.computers.Service.EventBus");
        Assert.Equal("tools.kot.nk2.computers.Service.EventBus", id.Value);
        Assert.Equal("EventBus", id.Last);
    }

    [Fact]
    public void SlashOperatorAppendsParts() {
        var baseId = "tools.kot".AsId();
        var child = baseId / "child";
        Assert.Equal("tools.kot.child", child.Value);
    }

    [Fact]
    public void EqualityIsStructural() {
        Assert.Equal("a.b.c".AsId(), "a.b.c".AsId());
        Assert.NotEqual("a.b.c".AsId(), "a.b.d".AsId());
        Assert.Equal("a.b".AsId().GetHashCode(), "a.b".AsId().GetHashCode());
    }

    [Fact]
    public void RandomProducesRequestedLength() {
        Assert.Equal(6, Id.Random().Value.Length);
    }
}
