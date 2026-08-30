using System.Net.Sockets;
using Newtonsoft.Json.Linq;

namespace Computers.Tests.E2e;

public class E2eFailureException : Exception {
    public E2eFailureException(string error) : base(error) {
    }
}

// Talks the control protocol to one game process. One command in flight at a time.
public sealed class GameClient : IDisposable {
    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private int _nextCid = 1;

    public GameClient(int port) {
        _client = new TcpClient("127.0.0.1", port);
        var stream = _client.GetStream();
        stream.ReadTimeout = 90000;
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public JObject? Send(string cmd, object? args = null) {
        var cid = $"t{_nextCid++}";
        var request = args is null ? new JObject() : JObject.FromObject(args);
        request["cid"] = cid;
        request["cmd"] = cmd;
        _writer.WriteLine(request.ToString(Newtonsoft.Json.Formatting.None));

        while (true) {
            var line = _reader.ReadLine() ?? throw new IOException("the control connection closed");
            var reply = JObject.Parse(line);
            if (reply["re"]?.Value<string>() != cid) {
                continue;
            }
            if (!reply["ok"]!.Value<bool>()) {
                throw new E2eFailureException(reply["error"]?.Value<string>() ?? "unknown error");
            }
            return reply["data"] as JObject;
        }
    }

    public JObject Status() {
        return Send("status")!;
    }

    public string Place(string item, int x, int y, string? location = null) {
        return Send("place", new { item, x, y, location })!["id"]!.Value<string>()!;
    }

    public void PlaceChest(int x, int y, (string ItemId, int Count)[] items, string? location = null) {
        Send("placeChest", new { x, y, location, items = items.Select(i => new { itemId = i.ItemId, count = i.Count }).ToArray() });
    }

    public void WriteDisk(int x, int y, string path, string content, string? location = null) {
        Send("writeDisk", new { x, y, location, path, content });
    }

    public string ReadDisk(int x, int y, string path, string? location = null) {
        return Send("readDisk", new { x, y, location, path })!["content"]!.Value<string>()!;
    }

    public void RestartComputer(int x, int y, string? location = null) {
        Send("restartComputer", new { x, y, location });
    }

    public void WaitTicks(int count) {
        Send("waitTicks", new { count });
    }

    public JObject QueryObject(int x, int y, string? location = null) {
        return Send("queryObject", new { x, y, location })!;
    }

    public JObject QueryShippingBin() {
        return Send("queryShippingBin")!;
    }

    public void Interact(int x, int y, string? location = null) {
        Send("interact", new { x, y, location });
    }

    public void SaveGame() {
        Send("saveGame");
    }

    public string PollDisk(int x, int y, string path, TimeSpan deadline) {
        var until = DateTime.UtcNow + deadline;
        while (true) {
            try {
                return ReadDisk(x, y, path);
            } catch (E2eFailureException) when (DateTime.UtcNow < until) {
                Thread.Sleep(1000);
            }
        }
    }

    public void Dispose() {
        _client.Dispose();
    }
}
