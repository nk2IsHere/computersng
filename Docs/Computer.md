# The computer

A computer is a big craftable. Place it anywhere and insert a Disk to boot it. The disk
carries the computer's identity, so breaking the computer drops the disk and inserting it
into another computer brings everything back, including files and network address.

Right click a running computer to open its screen. It shows a console where you type
JavaScript or dot commands, described in [Console](Console.md). Press F1 inside the screen
to reboot the computer and reload its code.

## How programs run

Each computer runs its own JavaScript engine (Jint with ES modules) driven by a shared
cooperative scheduler. Computers keep running while their screen is closed and while you
are elsewhere, so automation keeps working in the background.

A program paces itself with `await System.NextFrame()`, which resumes on the next frame.
A script that loops without awaiting anything runs against a statement budget and is
aborted when it exceeds it, then the engine restarts. Long running programs should await
`System.NextFrame()` or `System.Delay(ms)` inside their loops.

The OS entrypoint is `/Entrypoint.js` in the core library. On boot it builds the console,
runs `/Startup.js` if that file exists on the computer, and then enters the event loop.
Write your own automation in `/Startup.js`, or run scripts manually with `.exec`.

## The JS environment

Host globals are documented in `Computers/assets/Library/Entrypoint.d.ts`:

- `System` - frame pacing, delays, module loading, clipboard, the computer id
- `Render` - draw primitives for the screen
- `Event` - the raw input and network event queue, normally pumped by the OS loop
- `Storage` - the layered file system, see [Storage](Storage.md)
- `Network` - LAN datagrams and HTTP, see [Networking](Networking.md)

Library modules load with `System.LoadModule(path)` or ES imports from other modules.
The core library lives under `/Core` and `/View` and your own files can shadow it, see
[Storage](Storage.md).
