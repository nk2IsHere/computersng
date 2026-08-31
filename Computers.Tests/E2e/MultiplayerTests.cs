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
    public void FarmhandOpensTheScreenAndTypesIntoIt() {
        var host = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var (x, y) = (ex, ey + 2);
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

        farmhand.Interact(x, y);
        WaitFor(() => farmhand.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "the remote screen never opened");

        farmhand.MenuKey(65);
        WaitFor(() => Waits.DiskContains(host, x, y, "/keys.txt", "65,"), TimeSpan.FromSeconds(30), "the farmhand's key never reached the computer");

        farmhand.MenuKey(27);
        WaitFor(() => farmhand.QueryActiveMenu() is null, TimeSpan.FromSeconds(30), "the remote screen never closed");

        farmhand.Interact(x, y);
        WaitFor(() => farmhand.QueryActiveMenu() == "GameWindow", TimeSpan.FromSeconds(30), "the remote screen never reopened");
        farmhand.MenuKey(66);
        WaitFor(() => Waits.DiskContains(host, x, y, "/keys.txt", "66,"), TimeSpan.FromSeconds(30), "typing after the reopen never landed");
    }
}
