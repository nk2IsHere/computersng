# Testing

## Unit suite

```
DOTNET_ROOT_X64=$HOME/.dotnet-x64 dotnet test --arch x64
```

Runs everything in `Computers.Tests` except the e2e tests, which show as skipped
unless opted in.

## E2E suite

```
COMPUTERS_E2E=1 DOTNET_ROOT_X64=$HOME/.dotnet-x64 dotnet test --arch x64 --filter Category=E2E
```

Runs only where the game is installed. Do not start it while a manually
launched game is open.

## The harness mod

All e2e code lives in the separate mod project `Computers.E2e`, deployed as its own
mod folder beside the main mod. It serves a localhost TCP control channel
that executes JSON commands on the game thread as per tick polled operations.

Environment variables read by the harness mod:

| Variable | Meaning |
|---|---|
| `COMPUTERS_E2E` | enables the harness |
| `COMPUTERS_E2E_PORT` | control channel port, default 6161 |
| `COMPUTERS_E2E_NEW_SAVE` | create a fresh farm with this name at the title screen |
| `COMPUTERS_E2E_LOAD_SAVE` | load an existing save by name prefix instead |
| `COMPUTERS_E2E_HOST` | run the world as a LAN co-op host with one starting cabin |
| `COMPUTERS_E2E_JOIN` | join that address as a farmhand instead of creating or loading |

`COMPUTERS_DEV_LOAD_SAVE` stays reserved for manual dev testing and the harness never
reads or sets it, so dev saves are never touched by e2e runs.

## Control protocol

Request `{cid, cmd, ...args}`, reply
`{re, ok, data}` or `{re, ok: false, error}`, one JSON object per line.
Tile addressed commands take an optional `location` defaulting to the farm.

| Command | Args | Purpose |
|---|---|---|
| `status` | none | game mode, save loaded, player count and `worldReady`, which only turns true once a save load has settled |
| `place` | `item`, `x`, `y` | places a big craftable via short name, a computer also gets a disk inserted, replies with the created id |
| `placeChest` | `x`, `y`, `items` | places a chest stocked with `{itemId, count}` items |
| `remove` | `x`, `y` | removes the object at the tile |
| `writeDisk` | `x`, `y`, `path`, `content` | writes a file into the computer's persistent storage |
| `readDisk` | `x`, `y`, `path` | reads a file from the computer's persistent storage |
| `restartComputer` | `x`, `y` | stops the computer, waits for the slice to drain and starts it again |
| `interact` | `x`, `y` | performs the player interaction with the object |
| `queryObject` | `x`, `y` | item id and mod id at the tile, plus chest contents when it is a chest |
| `waitTicks` | `count` | replies after that many game ticks |
| `queryShippingBin` | none | contents of the farm shipping bin |
| `saveGame` | none | runs the real end of day save, replies once saved |
| `quit` | none | exits the game cleanly |
