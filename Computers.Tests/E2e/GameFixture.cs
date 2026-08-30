using System.Diagnostics;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;

namespace Computers.Tests.E2e;

// Boots one game process for the whole e2e collection. The world is created fresh at
// the title screen by the harness mod, so no save exists before the run and every save
// folder the run creates is deleted on dispose.
public sealed class GameFixture : IDisposable {
    private static readonly TimeSpan ConnectDeadline = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan WorldDeadline = TimeSpan.FromSeconds(120);

    private readonly string _gameDir;
    private readonly string _savesDir;
    private readonly string _saveName;
    private readonly List<Process> _processes = new();
    private readonly int _hostPort;
    private int _launchCounter;

    public GameClient Client { get; private set; }

    public GameFixture() {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPUTERS_E2E"))) {
            throw new InvalidOperationException("the e2e fixture requires COMPUTERS_E2E");
        }

        _gameDir = Environment.GetEnvironmentVariable("GAME_PATH") ?? "/Users/nk2/Desktop/SVDev.app/Contents/MacOS";
        _savesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/StardewValley/Saves");
        // Alphanumeric only, the game strips other characters from the farm name when it
        // names the save folder, which would break the prefix match on reload and cleanup.
        _saveName = $"e2e{Guid.NewGuid().ToString("N")[..8]}";
        _hostPort = FreePort();

        LaunchHost(newWorld: true);
        Client = Connect(_hostPort, waitForWorld: true);
    }

    private void LaunchHost(bool newWorld) {
        var env = new Dictionary<string, string> {
            ["COMPUTERS_E2E_PORT"] = _hostPort.ToString(),
            ["COMPUTERS_E2E_HOST"] = "1",
            [newWorld ? "COMPUTERS_E2E_NEW_SAVE" : "COMPUTERS_E2E_LOAD_SAVE"] = _saveName
        };
        Launch("host", env);
    }

    public void RestartGame() {
        Client.Dispose();
        KillAll();
        LaunchHost(newWorld: false);
        Client = Connect(_hostPort, waitForWorld: true);
    }

    public GameClient LaunchFarmhand() {
        var port = FreePort();
        var env = new Dictionary<string, string> {
            ["COMPUTERS_E2E_PORT"] = port.ToString(),
            ["COMPUTERS_E2E_JOIN"] = "localhost"
        };
        Launch("farmhand", env);
        // The farmhand has no world of its own to wait for, joining completes later.
        return Connect(port, waitForWorld: false);
    }

    private void Launch(string role, Dictionary<string, string> extraEnv) {
        var logPath = Path.Combine(AppContext.BaseDirectory, $"e2e-{role}-{_saveName}-{++_launchCounter}.log");
        var start = new ProcessStartInfo {
            FileName = "/bin/sh",
            Arguments = $"-c \"exec ./StardewModdingAPI > '{logPath}' 2>&1\"",
            WorkingDirectory = _gameDir,
            UseShellExecute = false
        };
        start.Environment["COMPUTERS_E2E"] = "1";
        start.Environment["SMAPI_DEVELOPER_MODE"] = "true";
        foreach (var (key, value) in extraEnv) {
            start.Environment[key] = value;
        }

        var process = Process.Start(start) ?? throw new InvalidOperationException("the game process did not start");
        _processes.Add(process);
    }

    private static GameClient Connect(int port, bool waitForWorld) {
        GameClient? client = null;
        var deadline = DateTime.UtcNow + ConnectDeadline;
        while (client is null) {
            try {
                client = new GameClient(port);
            } catch (SocketException) when (DateTime.UtcNow < deadline) {
                Thread.Sleep(2000);
            }
        }

        if (!waitForWorld) {
            return client;
        }

        var worldDeadline = DateTime.UtcNow + WorldDeadline;
        while (true) {
            var status = client.Status();
            if (status["worldReady"]!.Value<bool>()) {
                return client;
            }
            if (DateTime.UtcNow >= worldDeadline) {
                throw new TimeoutException($"the world did not come up, last status {status}");
            }
            Thread.Sleep(2000);
        }
    }

    private static int FreePort() {
        var probe = new TcpListener(System.Net.IPAddress.Loopback, 0);
        probe.Start();
        var port = ((System.Net.IPEndPoint) probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private void KillAll() {
        foreach (var process in _processes) {
            try {
                if (!process.HasExited) {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(15000);
                }
            } catch (Exception) {
                // Disposal keeps going even if one process is already gone.
            }
        }
        _processes.Clear();
    }

    public void Dispose() {
        KillAll();
        if (!Directory.Exists(_savesDir)) {
            return;
        }
        foreach (var folder in Directory.EnumerateDirectories(_savesDir)) {
            if (Path.GetFileName(folder).StartsWith($"{_saveName}_")) {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
