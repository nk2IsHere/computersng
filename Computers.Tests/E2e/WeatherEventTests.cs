using Xunit;

namespace Computers.Tests.E2e;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class WeatherEventTests {
    private readonly GameFixture _fixture;

    public WeatherEventTests(GameFixture fixture) {
        _fixture = fixture;
    }

    // Subscribes to the station's day event and appends every event push to disk.
    private const string Program =
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { AddListener } from \"./Core/Events\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const append = (line) => {\n" +
        "        const current = Exists(\"/pushes.txt\") ? ReadString(\"/pushes.txt\") : \"\"\n" +
        "        if (Exists(\"/pushes.txt\")) Delete(\"/pushes.txt\")\n" +
        "        WriteString(\"/pushes.txt\", current + line + \"\\n\")\n" +
        "    }\n" +
        "    AddListener((event) => {\n" +
        "        if (event.Type !== \"NetworkMessage\") {\n" +
        "            return false\n" +
        "        }\n" +
        "        try {\n" +
        "            const parsed = JSON.parse(event.Data[1])\n" +
        "            if (parsed.event !== undefined) {\n" +
        "                append(parsed.event)\n" +
        "            }\n" +
        "        } catch { }\n" +
        "        return false\n" +
        "    })\n" +
        "    const run = async () => {\n" +
        "        try {\n" +
        "            const station = ReadString(\"/station.txt\")\n" +
        "            await Call(station, \"subscribe\", { events: [\"day\"] })\n" +
        "            append(\"subscribed\")\n" +
        "        } catch (e) {\n" +
        "            append(\"error \" + String(e))\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    [E2eFact]
    public void DayChangePushesTheSubscribedEvent() {
        var client = _fixture.Client;
        var (ex, ey) = _fixture.FarmhouseEntry;
        var y = ey + 16;
        var (x, stationX) = (ex + 2, ex + 6);
        client.Place("computer", x, y);
        client.Place("router", ex + 4, y);
        var stationId = client.Place("weatherStation", stationX, y);
        var station = stationId[(stationId.LastIndexOf('.') + 1)..];

        client.WriteDisk(x, y, "/Startup.js", Program);
        client.WriteDisk(x, y, "/station.txt", station);
        client.RestartComputer(x, y);
        Waits.Until(
            () => TryRead(client, x, y, "/pushes.txt")?.Contains("subscribed") == true,
            TimeSpan.FromSeconds(60),
            "the subscription was never confirmed"
        );

        // The end of day save advances the day, which must push the day event.
        client.SaveGame();
        Waits.Until(
            () => TryRead(client, x, y, "/pushes.txt")?.Contains("day") == true,
            TimeSpan.FromSeconds(60),
            "the day event never arrived"
        );
    }

    private static string? TryRead(GameClient client, int x, int y, string path) {
        try {
            return client.ReadDisk(x, y, path);
        } catch (E2eFailureException) {
            return null;
        }
    }
}
