# Multiplayer

The mod is host authoritative. Computers, routers and peripherals exist only on the
host, run only on the host and persist only in the host's save.

## What farmhands can do

- Open any computer's screen and use it.
- Share a screen. Any number of players within the configured viewer limit can watch
  one computer at once and everyone's input interleaves, like a shared terminal.
- Place and remove computers, routers and peripherals.
- Insert a disk into a computer and initialize it.
- Click routers and peripherals to see their ids. On a farmhand the router's channel
  is not shown.

There is no farmhand side JS API interface, as well as computer storage.

## Configuration

Under `multiplayer:` in Configuration.yml. `castTicksPerFrame` throttles the stream,
`maxViewersPerComputer` caps shared screens, `callTimeoutTicks` bounds client calls.
