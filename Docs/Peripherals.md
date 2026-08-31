# Network and peripherals

This is the gameplay guide. The wire protocol reference is in [Networking](Networking.md).

## Setting up a network

Place a Router within 10 tiles of every computer and peripheral that should be online.
Routers within 16 tiles of each other mesh automatically. To bridge locations, set the
same channel on an Advanced Router in each location with
`.router-channel <address> <channel>`. Basic routers reject channels and only mesh by
radius.

Messages hop one router per game tick and are fire-and-forget, so distance costs time
and delivery is never guaranteed. Everything reliable is built on request and reply.

From a computer console:

- `.net-addr` prints your address
- `.net-ls` lists covering routers, discovers reachable endpoints and asks each what it is
- `.peripheral <address> <cmd> [json-args]` sends any command by hand

Routers answer the same RPC convention, so `.peripheral` works on them too:

```
.peripheral r1a2b3 ping
.peripheral r1a2b3 discover
.peripheral r1a2b3 configure {"channel":5}
.peripheral r1a2b3 configure {"channel":null}
```

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

Tiered peripherals come in two craftables. The basic version reaches only its four
adjacent tiles and the advanced version, costing an extra iridium bar, reaches the whole
edge-connected group or long range. `ping` tells you which one answered.

## Machine controller

Groups machines and chests connected edge to edge around it. `list` shows the group,
`insert` loads a machine from a chest, `collect` moves finished output into group chests
and the `ready` subscription pushes `machineReady` when a machine finishes.

```
.peripheral ab12ef ping
.machines ab12ef
.peripheral ab12ef list
.peripheral ab12ef rescan
.peripheral ab12ef insert {"machine":{"x":2,"y":7},"itemId":"378"}
.peripheral ab12ef insert {"machine":{"x":2,"y":7},"itemId":"378","fromChest":{"x":3,"y":7}}
.peripheral ab12ef collect {"machine":{"x":2,"y":7}}
.peripheral ab12ef collect {"machine":"all"}
.peripheral ab12ef subscribe {"events":["ready"]}
.peripheral ab12ef unsubscribe {"events":["ready"]}
```

## Weather station

`read` returns time, day, season, year, the station's local weather, tomorrow's weather
and daily luck. Subscribe to `day` for day changes and `time` for the ten minute clock.

```
.peripheral cd34ab ping
.peripheral cd34ab read
.peripheral cd34ab subscribe {"events":["day","time"]}
.peripheral cd34ab unsubscribe {"events":["time"]}
```

## Player sensor

`read` returns players and NPCs within the sensor's radius, 8 tiles by default. The basic
sensor caps at 8 tiles and the advanced one at 64. Set a
per-sensor radius with `configure`, which survives saves, and check it with `ping`.
Subscribe to `presence` for entered and left pushes.

```
.peripheral ef56cd ping
.peripheral ef56cd read
.peripheral ef56cd configure {"radius":16}
.peripheral ef56cd subscribe {"events":["presence"]}
.peripheral ef56cd unsubscribe {"events":["presence"]}
```

## Mailer

`notify` pops a HUD message immediately. `mail` queues a real in-game letter for
tomorrow's mailbox, which is how a far away base reports overnight.

```
.notify ab12cd The furnaces are done
.mail ab12cd Iron ran out yesterday
.peripheral ab12cd mail {"text":"Full report here","title":"Factory status"}
```

## Shipping controller

Sells items from chests on its four adjacent tiles into the shipping bin. Money arrives
with the nightly shipment like hand-shipped items.

```
.peripheral ef34ab ping
.peripheral ef34ab list
.price ef34ab 378
.ship ef34ab 378
.ship ef34ab 378 25
```

## Speaker

Plays game sound cues positioned at the speaker.

```
.play cd56ef furnace
.play cd56ef junimoMeep 1200
```
