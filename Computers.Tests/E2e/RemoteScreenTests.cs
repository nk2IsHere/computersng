using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class RemoteScreenTests {
    private readonly GameFixture _fixture;

    public RemoteScreenTests(GameFixture fixture) {
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
    public void HostAndFarmhandShareOneScreen() {
        var host = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex - 2, ey + 2);
        host.Place("computer", x, y);
        host.WriteDisk(x, y, "/Startup.js", Programs.KeyRecorder);
        host.RestartComputer(x, y);
        host.PollDisk(x, y, "/keys.txt", TimeSpan.FromSeconds(60));

        var farmhand = _fixture.LaunchFarmhand();
        WaitFor(() => host.Status()["playerCount"]!.Value<int>() >= 2, TimeSpan.FromSeconds(180), "the farmhand never joined");
        WaitFor(() => {
            try {
                return farmhand.Status()["worldReady"]!.Value<bool>();
            } catch (E2eFailureException) {
                return false;
            }
        }, TimeSpan.FromSeconds(120), "the farmhand's world never became ready");

        host.Interact(x, y);
        WaitFor(() => host.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "the host window never opened");
        farmhand.Interact(x, y);
        WaitFor(() => farmhand.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "the farmhand window never opened");

        host.MenuKey(72);
        farmhand.MenuKey(70);
        WaitFor(
            () => Waits.DiskContains(host, x, y, "/keys.txt", "72,") && Waits.DiskContains(host, x, y, "/keys.txt", "70,"),
            TimeSpan.FromSeconds(30),
            "keys from both players never landed"
        );

        Assert.Equal("GameWindow", host.QueryActiveMenu());
        Assert.Equal("GameWindow", farmhand.QueryActiveMenu());
        host.MenuKey(27);
        farmhand.MenuKey(27);
    }

    [E2eFact]
    public void TypingRoutesOnlyToTheViewedComputer() {
        var host = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var y = ey + 2;
        var (ax, bx) = (ex + 2, ex + 4);
        host.Place("computer", ax, y);
        host.Place("computer", bx, y);
        foreach (var x in new[] { ax, bx }) {
            host.WriteDisk(x, y, "/Startup.js", Programs.KeyRecorder);
            host.RestartComputer(x, y);
            host.PollDisk(x, y, "/keys.txt", TimeSpan.FromSeconds(60));
        }

        var farmhand = _fixture.LaunchFarmhand();
        WaitFor(() => host.Status()["playerCount"]!.Value<int>() >= 2, TimeSpan.FromSeconds(180), "the farmhand never joined");
        WaitFor(() => {
            try {
                return farmhand.Status()["worldReady"]!.Value<bool>();
            } catch (E2eFailureException) {
                return false;
            }
        }, TimeSpan.FromSeconds(120), "the farmhand's world never became ready");

        host.Interact(ax, y);
        WaitFor(() => host.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "the host window never opened");
        farmhand.Interact(bx, y);
        WaitFor(() => farmhand.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "the farmhand window never opened");

        host.MenuKey(81);
        farmhand.MenuKey(87);
        WaitFor(() => Waits.DiskContains(host, ax, y, "/keys.txt", "81,"), TimeSpan.FromSeconds(30), "the host key never landed");
        WaitFor(() => Waits.DiskContains(host, bx, y, "/keys.txt", "87,"), TimeSpan.FromSeconds(30), "the farmhand key never landed");

        Assert.DoesNotContain("87,", host.ReadDisk(ax, y, "/keys.txt"));
        Assert.DoesNotContain("81,", host.ReadDisk(bx, y, "/keys.txt"));
        host.MenuKey(27);
        farmhand.MenuKey(27);
    }
}
