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
{ event: "machineReady", machine: MachineSnapshot }
```

A message is a request when it has `cid` and `cmd` and no `re`. Replies echo the caller's
`cid` in `re`. The broadcast address `*` reaches every router and never reaches endpoints.

## Ping convention

Well-behaved endpoints answer `ping` with a self-description. Known types are `computer`
served by the OS by default, `machineController` and `router`. A crashed or unbooted
computer answers nothing. Override or extend your computer's answers by adding handlers to
`rpcServerView.handlers` in `Startup.js`.

## Router firmware commands

| Command | Args | Reply data |
|---|---|---|
| `ping` | | `{ type: "router", channel }` |
| `discover` | | `{ router: string, endpoints: string[] }` |
| `configure` | `{ channel: int or null }` | ack |

`discover` is typically sent to `*`. Every router that hears it answers with the endpoints
it covers, so collecting replies for longer sees farther. `configure` sets or clears the
router's channel. Routers within link radius mesh automatically and matching channels
bridge routers across locations.

## Machine controller commands

| Command | Args | Reply data |
|---|---|---|
| `ping` | | `{ type: "machineController" }` |
| `list` | | `GroupSnapshot` |
| `rescan` | | ack |
| `collect` | `{ machine: {x,y} or "all" }` | collected items |
| `insert` | `{ machine: {x,y}, itemId, count?, fromChest? }` | loaded item |
| `subscribe` | `{ events: ["ready"] }` | ack |
| `unsubscribe` | `{ events: ["ready"] }` | ack |

The controller groups machines and chests connected edge to edge around it. Subscribers
receive `machineReady` event pushes when a machine in the group becomes ready.

## Console commands

`.net-ls` lists covering routers, discovers reachable endpoints and pings each one for its
type. `.router-channel <address> <channel|clear>` configures a router and awaits its ack.
`.peripheral <address> <cmd> [json-args]` sends any RPC command. `.machines <address>`
shows a machine controller's group.
