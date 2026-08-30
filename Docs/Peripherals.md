# Network and peripherals

This is the gameplay guide. The wire protocol reference is in [Networking](Networking.md).

## Setting up a network

Place a Router within 10 tiles of every computer and peripheral that should be online.
Routers within 16 tiles of each other mesh automatically. To bridge locations, set the
same channel on a router in each location with `.router-channel <address> <channel>`.

Messages hop one router per game tick and are fire-and-forget, so distance costs time
and delivery is never guaranteed. Everything reliable is built on request and reply.

From a computer console:

- `.net-addr` prints your address
- `.net-ls` lists covering routers, discovers reachable endpoints and asks each what it is
- `.peripheral <address> <cmd> [json-args]` sends any command by hand

## Talking to peripherals from scripts

`Call` sends a command and awaits the reply. To receive event pushes, subscribe and then
register a listener that claims them before the OS loop logs them:

```js
import { Call } from "/Core/Rpc"
import { AddListener } from "/Core/Events"

const reading = await Call("ab12ef", "read", {})

await Call("ab12ef", "subscribe", { events: ["presence"] })
AddListener(event => {
    if (event.Type !== "NetworkMessage") return false
    const [from, payload] = event.Data
    if (from !== "ab12ef") return false
    const push = JSON.parse(payload)
    if (push.event === undefined) return false
    // react to the push here
    return true
})
```

Serve commands from your own computer by adding handlers in `/Startup.js`, where the OS
hands the script its `rpcServerView`:

```js
rpcServerView.handlers.set("greet", (args, from) => ({ hello: from }))
```

## Machine controller

Groups machines and chests connected edge to edge around it. `list` shows the group,
`insert` loads a machine from a chest, `collect` moves finished output into group chests
and the `ready` subscription pushes `machineReady` when a machine finishes.

```
.machines ab12ef
.peripheral ab12ef insert {"machine":{"x":2,"y":7},"itemId":"378"}
.peripheral ab12ef collect {"machine":"all"}
```

## Weather station

`read` returns time, day, season, year, the station's local weather, tomorrow's weather
and daily luck. Subscribe to `day` for day changes and `time` for the ten minute clock.

```
.peripheral cd34ab read
```

## Player sensor

`read` returns players and NPCs within the configured radius (`playerSensor.radius`,
default 8 tiles). Subscribe to `presence` for entered and left pushes.

```
.peripheral ef56cd read
```
