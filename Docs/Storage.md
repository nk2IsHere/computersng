# Storage

Every computer sees one file system composed of three layers. When the same path exists
in several layers, the highest layer wins, so your files can shadow and extend the
shipped library.

| Layer | Access | Where it lives |
|---|---|---|
| Persistent | read and write | Inside the game save, bound to the computer's disk |
| External | read only in game | `Mods/Computers/storage/<computer id>/` on your machine |
| Core | read only | The shipped library in `Mods/Computers/assets/Library/` |

`.ls` shows which layer each entry comes from.

## Persistent storage

This is the computer's own writable disk. `.write`, `.touch`, `.mkdir`, `.rm` and the
`Storage` API all write here. It is serialized into the game save with the computer state
and follows the Disk item around, so files survive breaking and re-placing the computer.

## External storage

External storage is for editing files in a real editor while the game runs. It maps a
folder on your machine into the computer read-only:

1. Run `.id` in the console to get the computer id.
2. Create that folder path under `Mods/Computers/storage/`. The id contains slashes and
   they become nested folders.
3. Put files there. They are visible in the console immediately, no restart needed.

The game never writes to external storage. To let a program modify such a file, copy it
into persistent storage first, for example `.cp /myScript.js /copy.js`.

## Shadowing and Startup.js

Module resolution walks the layers top down, so a file in persistent or external storage
replaces the core file at the same path. That is how you customize the OS:

- `/Startup.js` runs at every boot when present. Register RPC handlers, subscribe to
  peripherals or replace the whole OS loop here.
- `/Command/YourCommand.js` adds a console command.
- Any `/Core` or `/View` module can be replaced wholesale.

Press F1 after changing files that are already loaded, since modules cache per boot.

## The Storage API

Scripts use the `Storage` global (typed in `Entrypoint.d.ts`): `Exists`, `List`, `Read`,
`ReadMetadata`, `Write`, `Delete` and `MakeDirectory`, all returning a
`StorageResponse` with either data or a typed error. Friendlier wrappers live in
`/Core/Storage`.

Toggles live in `Configuration.yml` under `storage:` including `enablePersistentStorage`,
`enableExternalStorage` and `externalStorageFolder`.
