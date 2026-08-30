using System.Net.Sockets;
using Computers.E2e;
using Computers.E2e.Wire;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

public class E2eControlServiceTests : IDisposable {
    private sealed class ImmediateOperation : IPendingOperation {
        public bool TryComplete(out object? data, out string? error) {
            data = new StatusResult("title", false, 0, false);
            error = null;
            return true;
        }
    }

    private sealed class CountdownOperation : IPendingOperation {
        private int _remaining;

        public CountdownOperation(int ticks) {
            _remaining = ticks;
        }

        public bool TryComplete(out object? data, out string? error) {
            data = null;
            error = null;
            return --_remaining <= 0;
        }
    }

    private readonly E2eControlService _service;
    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    public E2eControlServiceTests() {
        var port = FreePort();
        _service = new E2eControlService(
            port,
            request => request switch {
                StatusRequest => new ImmediateOperation(),
                WaitTicksRequest wait => new CountdownOperation(wait.Count),
                _ => throw new E2eRequestException("unsupported in this test")
            },
            _ => { }
        );
        _service.Start();
        _client = new TcpClient("127.0.0.1", port);
        var stream = _client.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };
    }

    private static int FreePort() {
        var probe = new TcpListener(System.Net.IPAddress.Loopback, 0);
        probe.Start();
        var port = ((System.Net.IPEndPoint) probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private JObject ReadReplyAfterTicks(int maxTicks) {
        var read = Task.Run(() => _reader.ReadLine());
        for (var i = 0; i < maxTicks && !read.IsCompleted; i++) {
            _service.Tick();
            Thread.Sleep(10);
        }
        Assert.True(read.Wait(TimeSpan.FromSeconds(5)), "no reply arrived");
        return JObject.Parse(read.Result!);
    }

    [Fact]
    public void RepliesToStatusOnTick() {
        _writer.WriteLine("{\"cid\":\"c1\",\"cmd\":\"status\"}");
        var reply = ReadReplyAfterTicks(50);
        Assert.Equal("c1", reply["re"]!.Value<string>());
        Assert.True(reply["ok"]!.Value<bool>());
        Assert.Equal("title", reply["data"]!["gameMode"]!.Value<string>());
    }

    [Fact]
    public void MultiTickOperationRepliesAfterEnoughTicks() {
        _writer.WriteLine("{\"cid\":\"c2\",\"cmd\":\"waitTicks\",\"count\":3}");
        var reply = ReadReplyAfterTicks(50);
        Assert.Equal("c2", reply["re"]!.Value<string>());
        Assert.True(reply["ok"]!.Value<bool>());
    }

    [Fact]
    public void ParseErrorRepliesWithoutTicks() {
        _writer.WriteLine("{\"cid\":\"c3\",\"cmd\":\"nope\"}");
        var read = Task.Run(() => _reader.ReadLine());
        Assert.True(read.Wait(TimeSpan.FromSeconds(5)), "no reply arrived");
        var reply = JObject.Parse(read.Result!);
        Assert.False(reply["ok"]!.Value<bool>());
        Assert.Equal("unknown command 'nope'", reply["error"]!.Value<string>());
    }

    public void Dispose() {
        _client.Dispose();
        _service.Dispose();
    }
}
