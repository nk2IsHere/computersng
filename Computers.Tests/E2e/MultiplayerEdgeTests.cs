using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class MultiplayerEdgeTests {
    private readonly GameFixture _fixture;

    public MultiplayerEdgeTests(GameFixture fixture) {
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

    private GameClient JoinFarmhand(GameClient host) {
        var farmhand = _fixture.LaunchFarmhand();
        WaitFor(() => host.Status()["playerCount"]!.Value<int>() >= 2, TimeSpan.FromSeconds(180), "the farmhand never joined");
        WaitFor(() => FarmhandReady(farmhand), TimeSpan.FromSeconds(120), "the farmhand's world never became ready");
        return farmhand;
    }

    private static bool FarmhandReady(GameClient farmhand) {
        try {
            return farmhand.Status()["worldReady"]!.Value<bool>();
        } catch (E2eFailureException) {
            return false;
        }
    }

    [E2eFact]
    public void CastSurvivesARebootAndEndsWithTheComputer() {
        var host = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 4, ey + 2);
        host.Place("computer", x, y);
        host.WriteDisk(x, y, "/Startup.js", Programs.KeyRecorder);
        host.RestartComputer(x, y);
        host.PollDisk(x, y, "/keys.txt", TimeSpan.FromSeconds(60));

        var farmhand = JoinFarmhand(host);
        farmhand.Interact(x, y);
        WaitFor(() => farmhand.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "the remote screen never opened");

        // A reboot swaps the engine under the viewer, typing afterwards must still land.
        host.RestartComputer(x, y);
        host.PollDisk(x, y, "/keys.txt", TimeSpan.FromSeconds(60));
        farmhand.MenuKey(74);
        WaitFor(() => Waits.DiskContains(host, x, y, "/keys.txt", "74,"), TimeSpan.FromSeconds(30), "typing after the reboot never landed");

        // Removing the computer must close the viewer's window and leave the host healthy.
        host.Remove(x, y);
        WaitFor(() => farmhand.QueryActiveMenu() is null, TimeSpan.FromSeconds(30), "the window never closed after removal");
        Assert.True(host.Status()["worldReady"]!.Value<bool>());
    }

    [E2eFact]
    public void FarmhandCreatesAWorkingComputer() {
        var host = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 6, ey + 2);

        var farmhand = JoinFarmhand(host);
        farmhand.Send("place", new { item = "computer", x, y });
        WaitFor(() => {
            try {
                host.QueryObject(x, y);
                return true;
            } catch (E2eFailureException) {
                return false;
            }
        }, TimeSpan.FromSeconds(30), "the farmhand's placement never synced to the host");

        farmhand.InsertDisk(x, y);
        WaitFor(() => {
            try {
                return host.QueryObject(x, y)["id"]?.Value<string>() is not null;
            } catch (E2eFailureException) {
                return false;
            }
        }, TimeSpan.FromSeconds(60), "the host never stamped the farmhand's disk");

        host.WriteDisk(x, y, "/Startup.js", Programs.KeyRecorder);
        host.RestartComputer(x, y);
        host.PollDisk(x, y, "/keys.txt", TimeSpan.FromSeconds(60));

        farmhand.Interact(x, y);
        WaitFor(() => farmhand.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "the screen of the created computer never opened");
        farmhand.MenuKey(75);
        WaitFor(() => Waits.DiskContains(host, x, y, "/keys.txt", "75,"), TimeSpan.FromSeconds(30), "typing on the created computer never landed");
        farmhand.MenuKey(27);
    }

    [E2eFact]
    public void DisconnectWhileViewingLeavesTheHostHealthy() {
        var host = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 8, ey + 2);
        host.Place("computer", x, y);
        host.WriteDisk(x, y, "/Startup.js", Programs.KeyRecorder);
        host.RestartComputer(x, y);
        host.PollDisk(x, y, "/keys.txt", TimeSpan.FromSeconds(60));

        var farmhand = JoinFarmhand(host);
        farmhand.Interact(x, y);
        WaitFor(() => farmhand.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "the remote screen never opened");

        _fixture.KillFarmhands();
        WaitFor(() => host.Status()["playerCount"]!.Value<int>() <= 1, TimeSpan.FromSeconds(120), "the host never noticed the disconnect");

        // The computer keeps running and the host stays responsive.
        Assert.True(host.Status()["worldReady"]!.Value<bool>());
        host.WriteDisk(x, y, "/probe.txt", "alive");
        Assert.Equal("alive", host.ReadDisk(x, y, "/probe.txt"));

        var second = JoinFarmhand(host);
        second.Interact(x, y);
        WaitFor(() => second.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "a fresh farmhand could not view after the disconnect");
        second.MenuKey(76);
        WaitFor(() => Waits.DiskContains(host, x, y, "/keys.txt", "76,"), TimeSpan.FromSeconds(30), "typing after the rejoin never landed");
        second.MenuKey(27);
    }

    [E2eFact]
    public void InvalidRequestsStayReadable() {
        var host = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex + 6, ey + 2);

        // A computer craftable without a disk is a machine without a computer, so a
        // farmhand opening it gets the structured error toast and no window.
        host.Send("place", new { item = "computerShell", x, y });
        var farmhand = JoinFarmhand(host);

        // The shared farmhand may carry an open window from an earlier test.
        if (farmhand.QueryActiveMenu() is not null) {
            farmhand.MenuKey(27);
            WaitFor(() => farmhand.QueryActiveMenu() is null, TimeSpan.FromSeconds(30), "the stale window never closed");
        }
        WaitFor(() => {
            try {
                farmhand.QueryObject(x, y);
                return true;
            } catch (E2eFailureException) {
                return false;
            }
        }, TimeSpan.FromSeconds(30), "the shell never synced to the farmhand");

        farmhand.Interact(x, y);
        Thread.Sleep(3000);
        Assert.Null(farmhand.QueryActiveMenu());
        Assert.True(farmhand.Status()["saveLoaded"]!.Value<bool>());
        Assert.True(host.Status()["worldReady"]!.Value<bool>());
    }
}
