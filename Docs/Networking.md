# Networking

Computers, routers and peripherals form a LAN. Every participant is an addressable actor
with a short address. Routers flood datagrams one hop per game tick with a TTL and
duplicate suppression, so delivery is never guaranteed and farther targets answer later.

The host `Network` API exposes only what a radio instantly knows. `SendMessage` is a
fire-and-forget datagram, `GetAddress` returns your own address and `GetRouters` lists the
covering routers. Everything else is a protocol built on datagrams. `Call` in `/Core/Rpc`
adds request and reply. `Discover` and `ConfigureRouter` in `/Core/Network` are protocols
over that. The `RpcServerView` in the OS view pipeline answers requests addressed to your
computer.

## Wire protocol

Payloads are JSON by convention.

Request datagram payload:

```
{ cid: string|number, cmd: string, ...args }
```

Reply datagram payload:

```
{ re: cid, ok: true, data: any }
{ re: cid, ok: false, error: string }
```

Event push payload:

```
{ event: "machineReady", machine: MachineMemberSnapshot }
```

A message is a request when it has `cid` and `cmd` and no `re`. Replies echo the caller's
`cid` in `re`. The broadcast address `*` reaches every router and never reaches endpoints.

## Ping convention

Well-behaved endpoints answer `ping` with a self-description. Known types are `computer`
served by the OS by default, `machineController`, `weatherStation`, `playerSensor`, `mailer`,
`shippingController`, `speaker` and `router`. A crashed or unbooted
computer answers nothing. Override or extend your computer's answers by adding handlers to
`rpcServerView.handlers` in `Startup.js`.

Tiered peripherals and routers also answer ping with a `tier` field, `"basic"` or
`"advanced"`. Tier 1 reaches adjacent tiles and tier 2 reaches the connected group or
long range. Advanced versions are separate craftables costing the base recipe plus an
iridium bar.

## Router firmware commands

| Command | Args | Reply data |
|---|---|---|
| `ping` | | `{ type: "router", tier, channel }` |
| `discover` | | `{ router: string, endpoints: string[] }` |
| `configure` | `{ channel: int or null }` | ack, basic routers reject it |

`discover` is typically sent to `*`. Every router that hears it answers with the endpoints
it covers, so collecting replies for longer sees farther. `configure` sets or clears the
router's channel. Routers within link radius mesh automatically and matching channels
bridge routers across locations.

## Machine controller commands

| Command | Args | Reply data |
|---|---|---|
| `ping` | | `{ type: "machineController", tier }` |
| `list` | | `GroupSnapshot` with one `members` list. Each member carries `kind: "machine"`, `"chest"` or `"connector"` |
| `rescan` | | ack |
| `collect` | `{ machine: {x,y} or "all" }` | collected items |
| `insert` | `{ machine: {x,y}, itemId, count?, fromChest? }` | loaded item |
| `subscribe` | `{ events: ["ready"] }` | ack |
| `unsubscribe` | `{ events: ["ready"] }` | ack |

The controller groups machines and chests connected edge to edge around it. The basic
controller reaches only its four adjacent tiles, the advanced one the whole group. Subscribers
receive `machineReady` event pushes when a machine in the group becomes ready.

## Weather station commands

| Command | Args | Reply data |
|---|---|---|
| `ping` | | `{ type: "weatherStation" }` |
| `read` | | `{ time, day, season, year, weather, weatherTomorrow, dailyLuck }` |
| `subscribe` | `{ events: ["day", "time"] }` | ack |
| `unsubscribe` | `{ events: [...] }` | ack |

The station reads its own location's weather, so an Island station reports Island rain.
Subscribers receive `{ event: "day" | "time", reading }` pushes when the day or the ten
minute game clock changes.

## Player sensor commands

| Command | Args | Reply data |
|---|---|---|
| `ping` | | `{ type: "playerSensor", tier, radius }` |
| `read` | | `{ players: [{ kind, name, x, y }], npcs: [...] }` |
| `configure` | `{ radius: 1..cap }` | ack, cap 8 basic and 64 advanced |
| `subscribe` | `{ events: ["presence"] }` | ack |
| `unsubscribe` | `{ events: [...] }` | ack |

The sensor sees players and NPCs within its radius. The default comes from
`playerSensor.radius` (8 tiles) and `configure` overrides it per sensor, surviving saves. Subscribers receive `{ event: "presence", entered: [...], left: [...] }`
pushes when someone enters or leaves the radius.

## Mailer commands

| Command | Args | Reply data |
|---|---|---|
| `ping` | | `{ type: "mailer" }` |
| `notify` | `{ text, error? }` | ack |
| `mail` | `{ text, title? }` | `{ mailId }` |

`notify` shows a HUD message right away. `mail` queues an in-game letter that arrives in
tomorrow's mailbox. Letters persist with the mailer, so they survive saves.

## Shipping controller commands

| Command | Args | Reply data |
|---|---|---|
| `ping` | | `{ type: "shippingController", tier }` |
| `list` | | `{ chests: [{ x, y, items }] }` |
| `price` | `{ itemId }` | `{ itemId, price }` |
| `sell` | `{ itemId, count? }` | `{ itemId, name, sold, value }` |

The controller sells group chests into the shipping bin, scanning the same edge-connected
layout as the machine controller. The basic controller reaches only its four adjacent
tiles, the advanced one the whole group. `sell` without a count sells everything found. The money arrives with the nightly shipment.

## Speaker commands

| Command | Args | Reply data |
|---|---|---|
| `ping` | | `{ type: "speaker" }` |
| `play` | `{ cue, pitch? }` | ack |

Plays a game sound cue positioned at the speaker. Unknown cues reply with an error.

## Console commands

`.net-ls` lists covering routers, discovers reachable endpoints and pings each one for its
type. `.router-channel <address> <channel|clear>` configures a router and awaits its ack.
`.peripheral <address> <cmd> [json-args]` sends any RPC command. `.machines <address>`
shows a machine controller's group.
