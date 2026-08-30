using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Computers.E2e.Wire;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;

namespace Computers.E2e;

// Serves the e2e control channel. A background thread reads newline delimited JSON
// requests from one client. Parsed requests become pending operations that run only
// inside Tick, which the mod calls on the game update tick, so command execution is
// always on the game thread.
public sealed class E2eControlService : IDisposable {
    private const int OperationDeadlineTicks = 3600;

    private readonly int _port;
    private readonly Func<E2eRequest, IPendingOperation> _createOperation;
    private readonly Action<string> _log;

    private readonly ConcurrentQueue<(JToken Cid, E2eRequest Request)> _incoming = new();
    private readonly List<(JToken Cid, IPendingOperation Operation, int StartedAtTick)> _live = new();
    private readonly object _writeLock = new();

    private TcpListener? _listener;
    private StreamWriter? _writer;
    private Thread? _readThread;
    private volatile bool _disposed;
    private int _tick;

    public E2eControlService(int port, Func<E2eRequest, IPendingOperation> createOperation, Action<string> log) {
        _port = port;
        _createOperation = createOperation;
        _log = log;
    }

    public void Start() {
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();
        _readThread = new Thread(AcceptAndRead) { IsBackground = true, Name = "Computers e2e control" };
        _readThread.Start();
        _log($"E2e control listening on port {_port}.");
    }

    private void AcceptAndRead() {
        while (!_disposed) {
            TcpClient client;
            try {
                client = _listener!.AcceptTcpClient();
            } catch (Exception) {
                if (_disposed) {
                    return;
                }
                continue;
            }

            _log("E2e client connected.");
            try {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                lock (_writeLock) {
                    _writer = new StreamWriter(stream) { AutoFlush = true };
                }
                string? line;
                while ((line = reader.ReadLine()) != null) {
                    HandleLine(line);
                }
            } catch (Exception exception) {
                if (!_disposed) {
                    _log($"E2e client dropped. {exception.Message}");
                }
            } finally {
                lock (_writeLock) {
                    _writer = null;
                }
            }
        }
    }

    private void HandleLine(string line) {
        JObject payload;
        try {
            payload = JObject.Parse(line);
        } catch (Exception) {
            WriteReply(Reply.Failure(JValue.CreateNull(), "request is not valid JSON"));
            return;
        }

        WireRequests.TryReadCid(payload, out var cid);
        var cmd = payload["cmd"]?.Value<string>();
        if (cmd is null) {
            WriteReply(Reply.Failure(cid, "'cmd' is required"));
            return;
        }

        try {
            var request = Wire.RequestParser.Parse(cmd, payload);
            _incoming.Enqueue((cid, request));
        } catch (E2eRequestException exception) {
            WriteReply(Reply.Failure(cid, exception.Message));
        }
    }

    public void Tick() {
        _tick++;

        while (_incoming.TryDequeue(out var entry)) {
            try {
                _live.Add((entry.Cid, _createOperation(entry.Request), _tick));
            } catch (Exception exception) {
                WriteReply(Reply.Failure(entry.Cid, exception.Message));
            }
        }

        // Poll in arrival order so pipelined commands from one client execute in the
        // order they were sent.
        var finishedIndexes = new List<int>();
        for (var index = 0; index < _live.Count; index++) {
            var (cid, operation, startedAt) = _live[index];
            bool finished;
            object? data = null;
            string? error;
            try {
                finished = operation.TryComplete(out data, out error);
            } catch (Exception exception) {
                finished = true;
                error = exception.Message;
            }

            if (!finished && _tick - startedAt >= OperationDeadlineTicks) {
                finished = true;
                error = "operation timed out";
            }

            if (!finished) {
                continue;
            }

            finishedIndexes.Add(index);
            WriteReply(error is null ? Reply.Success(cid, data) : Reply.Failure(cid, error));
        }

        for (var i = finishedIndexes.Count - 1; i >= 0; i--) {
            _live.RemoveAt(finishedIndexes[i]);
        }
    }

    private void WriteReply(Reply reply) {
        lock (_writeLock) {
            try {
                _writer?.WriteLine(WireJson.Serialize(reply));
            } catch (Exception exception) {
                _log($"E2e reply write failed. {exception.Message}");
            }
        }
    }

    public void Dispose() {
        _disposed = true;
        _listener?.Stop();
    }
}
