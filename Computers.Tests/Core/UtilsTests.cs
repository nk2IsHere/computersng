using Computers.Core;
using Xunit;

namespace Computers.Tests.Core;

public class UtilsTests {
    [Fact]
    public void SaveEnvelopeRoundTripsWithTypePreservation() {
        var envelope = new Dictionary<Id, Dictionary<string, object>> {
            ["a.b.entry".AsId()] = new() {
                ["Id"] = "a.b.entry".AsId(),
                ["Storage"] = new Dictionary<string, object> { ["file"] = new byte[] { 1, 2, 3 } }
            }
        };

        var deserialized = envelope.Serialize().Deserialize<Dictionary<Id, Dictionary<string, object>>>();

        Assert.NotNull(deserialized);
        var entry = deserialized!["a.b.entry".AsId()];
        Assert.Equal("a.b.entry".AsId(), entry["Id"]);
        var storage = Assert.IsType<Dictionary<string, object>>(entry["Storage"]);
        Assert.Equal(new byte[] { 1, 2, 3 }, storage["file"]);
    }

    [Fact]
    public void IntegersComeBackAsLongs() {
        // Documents the Newtonsoft quirk that Restore implementations must handle
        // (see Convert.ToInt32 in RouterStatefulDataContextEntry.Restore).
        var deserialized = new Dictionary<string, object> { ["n"] = 7 }
            .Serialize()
            .Deserialize<Dictionary<string, object>>();

        Assert.IsType<long>(deserialized!["n"]);
    }

    [Fact]
    public void WhereNotNullFiltersNulls() {
        var items = new string?[] { "a", null, "b" };
        Assert.Equal(new[] { "a", "b" }, items.WhereNotNull());
    }
}
