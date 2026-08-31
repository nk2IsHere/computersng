namespace Computers.Tests.E2e;

public static class Programs {
    // Appends every key press event's key code to a disk file so tests can assert
    // which keys reached the computer, no matter which player typed them.
    public const string KeyRecorder =
        "import { AddListener } from \"./Core/Events\"\n" +
        "import { WriteString, ReadString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    if (Exists(\"/keys.txt\")) Delete(\"/keys.txt\")\n" +
        "    WriteString(\"/keys.txt\", \"\")\n" +
        "    AddListener((event) => {\n" +
        "        if (event.Type !== \"KeyPressed\") {\n" +
        "            return false\n" +
        "        }\n" +
        "        const current = ReadString(\"/keys.txt\")\n" +
        "        Delete(\"/keys.txt\")\n" +
        "        WriteString(\"/keys.txt\", current + String(event.Data[0]) + \",\")\n" +
        "        return false\n" +
        "    })\n" +
        "}\n";

    // Discovers the network, pings everything, reads weather when a station is in
    // reach, and stamps the output with the nonce from disk so reruns are tellable.
    public const string NetworkProbe =
        "import { Discover, GetRouters } from \"./Core/Network\"\n" +
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/probe.json\")) Delete(\"/probe.json\")\n" +
        "        WriteString(\"/probe.json\", JSON.stringify(data))\n" +
        "    }\n" +
    "    const run = async () => {\n" +
        "        const nonce = Exists(\"/nonce.txt\") ? ReadString(\"/nonce.txt\") : \"none\"\n" +
        "        const routers = GetRouters()\n" +
        "        try {\n" +
        "            const endpoints = await Discover()\n" +
        "            const types = {}\n" +
        "            let weather = null\n" +
        "            for (const address of endpoints) {\n" +
        "                try {\n" +
        "                    const info = await Call(address, \"ping\")\n" +
        "                    types[address] = info.type\n" +
        "                    if (info.type === \"weatherStation\" && weather === null) {\n" +
        "                        const reading = await Call(address, \"read\")\n" +
        "                        weather = reading.weather\n" +
        "                    }\n" +
        "                } catch (e) {\n" +
        "                    types[address] = \"error \" + String(e)\n" +
        "                }\n" +
        "            }\n" +
        "            write({ ok: true, nonce, routers, endpoints, types, weather })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, nonce, error: String(e), routers })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    // Configures a router to the channel read from disk. The router address comes from
    // an optional router file, falling back to the first covering router. A channel of
    // the word none clears it.
    public const string RouterConfigurer =
        "import { GetRouters, ConfigureRouter } from \"./Core/Network\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/configured.json\")) Delete(\"/configured.json\")\n" +
        "        WriteString(\"/configured.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        const nonce = Exists(\"/nonce.txt\") ? ReadString(\"/nonce.txt\") : \"none\"\n" +
        "        try {\n" +
        "            const raw = ReadString(\"/channel.txt\")\n" +
        "            const channel = raw === \"none\" ? null : parseInt(raw)\n" +
        "            const routers = GetRouters()\n" +
        "            const target = Exists(\"/router.txt\") ? ReadString(\"/router.txt\") : (routers.length > 0 ? routers[0].address : null)\n" +
        "            if (target === null) {\n" +
        "                write({ ok: false, nonce, error: \"no covering router\" })\n" +
        "                return\n" +
        "            }\n" +
        "            await ConfigureRouter(target, channel)\n" +
        "            write({ ok: true, nonce, channel })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, nonce, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    // Lists the machine controller group named in the controller file and stamps the
    // output with the nonce so reruns are tellable.
    public const string GroupLister =
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/group.json\")) Delete(\"/group.json\")\n" +
        "        WriteString(\"/group.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        const nonce = Exists(\"/nonce.txt\") ? ReadString(\"/nonce.txt\") : \"none\"\n" +
        "        try {\n" +
        "            const controller = ReadString(\"/controller.txt\")\n" +
        "            const group = await Call(controller, \"list\")\n" +
        "            write({ ok: true, nonce, group })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, nonce, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    // Runs the machine op edge sequence against the controller named on disk. Every
    // step records its outcome so the test can assert each one.
    public const string MachineOpEdges =
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/ops.json\")) Delete(\"/ops.json\")\n" +
        "        WriteString(\"/ops.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const attempt = async (fn) => {\n" +
        "        try {\n" +
        "            return { ok: true, data: await fn() }\n" +
        "        } catch (e) {\n" +
        "            return { ok: false, error: String(e) }\n" +
        "        }\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        const nonce = Exists(\"/nonce.txt\") ? ReadString(\"/nonce.txt\") : \"none\"\n" +
        "        try {\n" +
        "            const controller = ReadString(\"/controller.txt\")\n" +
        "            const machine = JSON.parse(ReadString(\"/machine.txt\"))\n" +
        "            const unknownItem = await attempt(() => Call(controller, \"insert\", { machine, itemId: \"999999\", count: 1 }))\n" +
        "            const collectEmpty = await attempt(() => Call(controller, \"collect\", { machine }))\n" +
        "            const insert = await attempt(() => Call(controller, \"insert\", { machine, itemId: \"378\", count: 1 }))\n" +
        "            const insertBusy = await attempt(() => Call(controller, \"insert\", { machine, itemId: \"378\", count: 1 }))\n" +
        "            write({ ok: true, nonce, unknownItem, collectEmpty, insert, insertBusy })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, nonce, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";

    // Tries sensor radius values against the sensor named on disk and records each
    // outcome, the accepted value first and the rejected one second.
    public const string SensorCapProbe =
        "import { Call } from \"./Core/Rpc\"\n" +
        "import { ReadString, WriteString, Exists, Delete } from \"./Core/Storage\"\n" +
        "export async function Main() {\n" +
        "    const write = (data) => {\n" +
        "        if (Exists(\"/sensor.json\")) Delete(\"/sensor.json\")\n" +
        "        WriteString(\"/sensor.json\", JSON.stringify(data))\n" +
        "    }\n" +
        "    const attempt = async (fn) => {\n" +
        "        try {\n" +
        "            return { ok: true, data: await fn() }\n" +
        "        } catch (e) {\n" +
        "            return { ok: false, error: String(e) }\n" +
        "        }\n" +
        "    }\n" +
        "    const run = async () => {\n" +
        "        const nonce = Exists(\"/nonce.txt\") ? ReadString(\"/nonce.txt\") : \"none\"\n" +
        "        try {\n" +
        "            const sensor = ReadString(\"/sensor.txt\")\n" +
        "            const limits = JSON.parse(ReadString(\"/limits.txt\"))\n" +
        "            const accepted = await attempt(() => Call(sensor, \"configure\", { radius: limits.accepted }))\n" +
        "            const rejected = await attempt(() => Call(sensor, \"configure\", { radius: limits.rejected }))\n" +
        "            const info = await attempt(() => Call(sensor, \"ping\"))\n" +
        "            write({ ok: true, nonce, accepted, rejected, info })\n" +
        "        } catch (e) {\n" +
        "            write({ ok: false, nonce, error: String(e) })\n" +
        "        }\n" +
        "    }\n" +
        "    run()\n" +
        "}\n";
}
