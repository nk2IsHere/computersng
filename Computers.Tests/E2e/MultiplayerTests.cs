using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class MultiplayerTests {
    private readonly GameFixture _fixture;

    public MultiplayerTests(GameFixture fixture) {
        _fixture = fixture;
    }

    [E2eFact]
    public void FarmhandSeesAndPokesTheHostsComputer() {
        var host = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex, ey + 2);
        host.Place("computer", x, y);

        var farmhand = _fixture.LaunchFarmhand();

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(180);
        while (DateTime.UtcNow < deadline) {
            if (host.Status()["playerCount"]!.Value<int>() >= 2) {
                break;
            }
            Thread.Sleep(2000);
        }
        Assert.True(host.Status()["playerCount"]!.Value<int>() >= 2, "the farmhand never joined");

        var hostView = host.QueryObject(x, y);
        var farmhandView = farmhand.QueryObject(x, y);
        Assert.Equal(hostView["itemId"]!.Value<string>(), farmhandView["itemId"]!.Value<string>());

        // The mod has no multiplayer support yet. Mod entities live in the host's save
        // and are never synced to clients, so a farmhand poking a computer fails with a
        // missing context entry instead of opening the screen. This assertion pins that
        // known gap and should flip to a plain success once multiplayer support lands.
        var missing = Assert.Throws<E2eFailureException>(() => farmhand.Interact(x, y));
        Assert.Contains("not found", missing.Message);
    }
}
