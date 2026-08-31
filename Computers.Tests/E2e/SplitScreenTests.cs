using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class SplitScreenTests {
    private readonly GameFixture _fixture;

    public SplitScreenTests(GameFixture fixture) {
        _fixture = fixture;
    }

    private static void WaitFor(Func<bool> condition, TimeSpan deadline, string what) {
        var until = DateTime.UtcNow + deadline;
        while (!condition()) {
            if (DateTime.UtcNow >= until) {
                throw new TimeoutException(what);
            }
            Thread.Sleep(300);
        }
    }

    [E2eFact]
    public void SplitScreenPlayerViewsAndTypesAndARemoteViewerJoinsIn() {
        var host = _fixture.Client;
        // Earlier tests may have left a remote farmhand connected. Split screen claims
        // its own cabin, so this scenario starts from a clean farmhand topology.
        _fixture.KillFarmhands();
        WaitFor(() => host.Status()["playerCount"]!.Value<int>() <= 1, TimeSpan.FromSeconds(120), "the old farmhand never left");

        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex + 2, ey + 4);
        host.Place("computer", x, y);
        host.WriteDisk(x, y, "/Startup.js", Programs.KeyRecorder);
        host.RestartComputer(x, y);
        host.PollDisk(x, y, "/keys.txt", TimeSpan.FromSeconds(60));

        var playersBeforeSplit = host.Status()["playerCount"]!.Value<int>();
        host.StartSplitScreen();
        WaitFor(() => host.Status()["playerCount"]!.Value<int>() > playersBeforeSplit, TimeSpan.FromSeconds(180), "the split screen player never joined");

        // Give the fresh instance a moment to finish entering the world before poking it.
        Thread.Sleep(5000);

        host.Interact(x, y, screen: 1);
        WaitFor(() => host.QueryActiveMenu(screen: 1) == "GameWindow", TimeSpan.FromSeconds(30), "the split screen window never opened");

        host.MenuKey(78, screen: 1);
        WaitFor(() => Waits.DiskContains(host, x, y, "/keys.txt", "78,"), TimeSpan.FromSeconds(30), "the split screen key never landed");

        // The three way topology, a remote farmhand views the same computer alongside
        // the split screen player.
        var farmhand = _fixture.LaunchFarmhand();
        WaitFor(() => {
            try {
                return farmhand.Status()["worldReady"]!.Value<bool>();
            } catch (E2eFailureException) {
                return false;
            }
        }, TimeSpan.FromSeconds(180), "the remote farmhand never joined the split session");

        farmhand.Interact(x, y);
        WaitFor(() => farmhand.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "the remote window never opened in the split session");
        farmhand.MenuKey(77);
        WaitFor(
            () => Waits.DiskContains(host, x, y, "/keys.txt", "77,") && Waits.DiskContains(host, x, y, "/keys.txt", "78,"),
            TimeSpan.FromSeconds(30),
            "keys from both viewers never landed"
        );

        host.MenuKey(27, screen: 1);
        farmhand.MenuKey(27);
    }
}
