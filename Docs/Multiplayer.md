# Multiplayer

The mod is host authoritative. Computers, routers and peripherals exist only on the
host, run only on the host and persist only in the host's save. Farmhands, remote and
split screen alike, interact through a message channel and never own entities.

## What farmhands can do

- Open any computer's screen and use it. The screen is mirrored from the host by
  replaying the frame's draw commands, so it is pixel perfect, and typing and clicks
  go back to the host and land in the computer exactly like local input.
- Share a screen. Any number of players within the configured viewer limit can watch
  one computer at once and everyone's input interleaves, like a shared terminal.
- Place and remove computers, routers and peripherals. World changes sync to the host,
  which does all entity management.
- Insert a disk into a computer. The client sends `initializeComputer` and the host
  stamps the synced disk with a computer id and starts it.
- Click routers and peripherals to see their ids. On a farmhand the router's channel
  is not shown, only the host knows it.

Computers never run on farmhands. There is no farmhand side JS API surface.

## The player channel

`Multiplayer/` holds the game free channel logic, `Game/Domain/SmapiPlayerTransport`
carries it over SMAPI mod messages. Requests are `{cid, cmd, v, ...args}`, replies
`{re, ok, data}` or `{re, ok: false, error}`, host to client events `{event, ...}`,
the same envelope as the mod's LAN wire. The `v` field is the protocol version, a
mismatch gets a readable error reply.

| Command | Sender | Purpose |
|---|---|---|
| `openScreen` | client | subscribe to a computer's screen by tile, replies with the computer id and canvas size |
| `closeScreen` | client | unsubscribe from a computer's screen |
| `screenInput` | client | one input event for a computer, kinds key, leftClick, rightClick and wheel |
| `initializeComputer` | client | ask the host to stamp and start a freshly inserted disk |
| `frame` | host event | one encoded screen frame, snapshot or delta |
| `screenClosed` | host event | the viewed computer is gone, close the window |

Adding a new player initiated feature means one request record, one parser case and
one registered host handler in `RegisterChannelHandlers`.

## Screen casting

The host taps each computer's frame publish. When a frame has viewers, the typed draw
command list is mirrored, the two raw pixel layers ride along run length encoded only
when they changed, and the whole payload is binary serialized and deflated by
`FrameCodec` into a few kilobytes. A new viewer first gets a full snapshot. Frames
only publish while the screen changes and only to active viewers, an idle or unwatched
computer costs nothing. The client composes pixels with the same `FrameComposer` and
its own copy of the font.

## Split screen

Fully supported. Interaction routes by player id, so a split screen player is an
ordinary viewer whose messages are delivered in process, the transport keeps a
registry of this process's screen players and bypasses the message bus for them. The
SMAPI to EventBus forwarding runs only on the main screen so entities tick exactly
once, and asset patching stays per screen because each screen has its own content
cache. Client channel state is per screen.

## Configuration

Under `multiplayer:` in Configuration.yml. `castTicksPerFrame` throttles the stream,
`maxViewersPerComputer` caps shared screens, `callTimeoutTicks` bounds client calls.
