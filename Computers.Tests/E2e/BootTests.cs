using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class BootTests {
    private readonly GameFixture _fixture;

    public BootTests(GameFixture fixture) {
        _fixture = fixture;
    }

    [E2eFact]
    public void GameBootsIntoAFreshWorld() {
        var status = _fixture.Client.Status();
        Assert.True(status["saveLoaded"]!.Value<bool>());
        Assert.Equal("playing", status["gameMode"]!.Value<string>());
    }
}
