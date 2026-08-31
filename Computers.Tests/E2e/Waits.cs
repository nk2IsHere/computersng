using Newtonsoft.Json.Linq;

namespace Computers.Tests.E2e;

public static class Waits {
    public static void Until(Func<bool> condition, TimeSpan deadline, string what) {
        var until = DateTime.UtcNow + deadline;
        while (!condition()) {
            if (DateTime.UtcNow >= until) {
                throw new TimeoutException(what);
            }
            Thread.Sleep(300);
        }
    }

    // True when the file exists and contains the needle, false on any read failure,
    // so wait loops survive the moment a program deletes and rewrites its output.
    public static bool DiskContains(GameClient client, int x, int y, string path, string needle) {
        try {
            return client.ReadDisk(x, y, path).Contains(needle);
        } catch (E2eFailureException) {
            return false;
        }
    }

    // Polls a JSON result file written by a nonce stamped program until the output for
    // the given nonce appears.
    public static JObject ResultForNonce(GameClient client, int x, int y, string path, string nonce, TimeSpan deadline, string? location = null) {
        var until = DateTime.UtcNow + deadline;
        while (true) {
            try {
                var result = JObject.Parse(client.ReadDisk(x, y, path, location));
                if (result["nonce"]?.Value<string>() == nonce) {
                    return result;
                }
            } catch (E2eFailureException) {
            }
            if (DateTime.UtcNow >= until) {
                throw new TimeoutException($"no {path} output for nonce {nonce} appeared");
            }
            Thread.Sleep(300);
        }
    }
}
