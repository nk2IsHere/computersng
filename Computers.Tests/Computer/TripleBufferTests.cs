using Computers.Computer.Utils;
using Xunit;

namespace Computers.Tests.Computer;

public class TripleBufferTests {
    private static TripleBuffer<int[]> Make() =>
        new(new[] { 1 }, new[] { 2 }, new[] { 3 });

    [Fact]
    public void ConsumeBeforeAnyPublishIsNotNew() {
        var buffer = Make();
        var (_, isNew) = buffer.Consume();
        Assert.False(isNew);
    }

    [Fact]
    public void PublishMakesNextConsumeNewExactlyOnce() {
        var buffer = Make();
        var produced = buffer.ProduceSlot;
        produced[0] = 42;
        buffer.Publish();

        var (slot, isNew) = buffer.Consume();
        Assert.True(isNew);
        Assert.Equal(42, slot[0]);

        var (slotAgain, isNewAgain) = buffer.Consume();
        Assert.False(isNewAgain);
        Assert.Same(slot, slotAgain); // same display buffer until next publish
    }

    [Fact]
    public void ProducerNeverGetsTheConsumersBuffer() {
        var buffer = Make();
        buffer.ProduceSlot[0] = 10;
        buffer.Publish();
        var (display, _) = buffer.Consume();

        // Produce/publish repeatedly; produce slot must never alias the held display buffer.
        for (var i = 0; i < 10; i++) {
            Assert.NotSame(display, buffer.ProduceSlot);
            buffer.Publish();
        }
    }

    [Fact]
    public void LatestPublishWins() {
        var buffer = Make();
        buffer.ProduceSlot[0] = 1;
        buffer.Publish();
        buffer.ProduceSlot[0] = 2;
        buffer.Publish();

        var (slot, isNew) = buffer.Consume();
        Assert.True(isNew);
        Assert.Equal(2, slot[0]);
    }
}
