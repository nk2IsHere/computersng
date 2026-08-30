# Testing

Two suites cover the mod. The unit suite runs the domain layers against test doubles
and needs no game beyond the referenced assemblies. The e2e suite boots the real game
and drives it over a control channel.

## Unit suite

```
DOTNET_ROOT_X64=$HOME/.dotnet-x64 dotnet test --arch x64
```

Runs everything in `Computers.Tests` except the e2e tests, which show as skipped
unless opted in. This is what CI runs.

## E2E suite

```
COMPUTERS_E2E=1 DOTNET_ROOT_X64=$HOME/.dotnet-x64 dotnet test --arch x64 --filter Category=E2E
```

Runs only where the game is installed, so never in CI. Never start it while a manually
launched game is open, the build deploys the mod and deploying over a running game
tears its assemblies. Game windows appear during the run and one full run costs a few
game boots of roughly a minute each.

Each run creates a brand new farm at runtime through the vanilla new game entry point,
so there is no committed save fixture and nothing drifts when the game updates. Save
folders created by a run are named `e2e<runId>_<uid>` and are deleted when the run
ends. Every game process writes its SMAPI log beside the test assembly as
`e2e-<role>-<save>-<n>.log`, which is the first place to look when a scenario fails.

## The harness mod

All e2e code lives in the separate mod project `Computers.E2e`, deployed as its own
mod folder beside the main mod. It does nothing at all unless `COMPUTERS_E2E` is set,
so it can stay installed during normal play. It serves a localhost TCP control channel
that executes JSON commands on the game thread as per tick polled operations.

Environment variables read by the harness mod:

| Variable | Meaning |
|---|---|
| `COMPUTERS_E2E` | enables the harness, absent means fully inert |
| `COMPUTERS_E2E_PORT` | control channel port, default 6161 |
| `COMPUTERS_E2E_NEW_SAVE` | create a fresh farm with this name at the title screen |
| `COMPUTERS_E2E_LOAD_SAVE` | load an existing save by name prefix instead |
| `COMPUTERS_E2E_HOST` | run the world as a LAN co-op host with one starting cabin |
| `COMPUTERS_E2E_JOIN` | join that address as a farmhand instead of creating or loading |

`COMPUTERS_DEV_LOAD_SAVE` stays reserved for manual dev testing and the harness never
reads or sets it, so dev saves are never touched by e2e runs.

## Control protocol

Same envelope as the mod's own network wire. Request `{cid, cmd, ...args}`, reply
`{re, ok, data}` or `{re, ok: false, error}`, camelCase, one JSON object per line.
Tile addressed commands take an optional `location` defaulting to the farm.

| Command | Args | Purpose |
|---|---|---|
| `status` | none | game mode, save loaded, player count and `worldReady`, which only turns true once a save load has settled |
| `place` | `item`, `x`, `y` | places a big craftable by short name through the real placement path, a computer also gets a disk inserted; replies with the created identity |
| `placeChest` | `x`, `y`, `items` | places a chest stocked with `{itemId, count}` items |
| `remove` | `x`, `y` | removes the object at the tile |
| `writeDisk` | `x`, `y`, `path`, `content` | writes a file into the computer's persistent storage |
| `readDisk` | `x`, `y`, `path` | reads a file from the computer's persistent storage |
| `restartComputer` | `x`, `y` | stops the computer, waits for the slice to drain and starts it again |
| `interact` | `x`, `y` | performs the player interaction with the object |
| `queryObject` | `x`, `y` | item id and mod identity at the tile, plus chest contents when it is a chest |
| `waitTicks` | `count` | replies after that many game ticks |
| `queryShippingBin` | none | contents of the farm shipping bin |
| `saveGame` | none | runs the real end of day save, replies once saved |
| `quit` | none | exits the game cleanly |

The short item names `place` accepts: computer, router, advancedRouter,
machineController, advancedMachineController, weatherStation, playerSensor,
advancedPlayerSensor, mailer, shippingController, advancedShippingController, speaker.

## Scenarios

Six tests in one xunit collection sharing a single game boot: the boot handshake, a
computer running a startup program, a network round trip through a router to the
weather station, a sell through the shipping controller landing in the real shipping
bin, a save and cold reload asserting disk files, identities, router channel and
sensor radius survive, and a multiplayer smoke test where a second game process joins
as a farmhand and sees the host's objects. The multiplayer test pins the current gap
that mod entities are not synced to clients, a farmhand interacting with a computer
receives a structured missing entity error.

## Bugs this suite has caught

Kept here as a reminder of why the suite exists. Endpoint datagram deduplication, the
same request delivered once per covering router used to execute non idempotent
commands twice. The storage restore split brain, a computer restored from a save
served its scripts an orphaned empty file system while the host and the save used the
restored one. Farmhand identity clobbering, a client running the host's entity
management overwrote net synced mod identities.
