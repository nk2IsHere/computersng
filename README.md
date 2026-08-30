# Computers

![Latest release](https://img.shields.io/github/v/release/nk2IsHere/computersng)
![CI](https://github.com/nk2IsHere/computersng/actions/workflows/ci.yml/badge.svg)

Programmable computers for Stardew Valley. Fourth attempt.

![title](https://github.com/nk2IsHere/computersng/blob/main/Docs/Readme/Title.png?raw=true)

## Goals.

### Generally:

Make processing of Stardew's resources a configurable function pipe-like process by introducing programming.

### Specifically this mod should:

- Add computer bigcraftable (can be placed anywhere) which
    - is interactable by player
    - has screen and input console
    - supports some interpreted programming language (python? lua? - undecided)
    - has apis that allow it to interact with other storages/machines..
    - auto-discovers machines indirectly connected to it (meaning that machines may form a group by attaching to each
      other and computer)
    - has peripherals


- Add monitor bigcraftable (can be placed anywhere) which
    - will have its own api to display requested data
    - will be of different sizes
    - will have to be connected directly to computer to operate


- Add peripherals:
    - Wireless stations - allows communication with other computers on a limited distance
    - Machine controller - discovers and drives a group of adjacent machines and chests
    - Weather station - reads time, day, season, weather, forecast and luck, pushes day/season/rain events
    - Player sensor - detects player/NPC presence within radius, pushes proximity events
    - Mailer - shows HUD toasts and queues in-game mail from scripts
    - Shipping controller - sells items from adjacent chests through the bin, queries prices
    - Speaker - plays game sound cues and music from scripts
    - Switchable devices - controllable sprinklers and lights

## Docs

- [Computer](Docs/Computer.md) - placing computers, disks, how programs run and the JS environment
- [Console and commands](Docs/Console.md) - the console, JS evaluation, piping and every command
- [Storage](Docs/Storage.md) - the layered file system, external storage and Startup.js
- [Networking](Docs/Networking.md) - the LAN wire protocol, router firmware and peripheral command tables
- [Peripherals](Docs/Peripherals.md) - setting up routers and using every peripheral

## TODOs

### Basic item support

- [x] Add bigcraftable type
- [x] Add craftingrecipe type
- [x] Add support for functional redux-like store
- [x] Create dataStore for patcher
- [x] Add support for figuring out the next ids for mod's bigcraftables
- [x] Add dictionary (Data/BigCraftablesInformation, Data/CraftingRecipes) patching
- [x] Add tilesheet (TileSheets/Craftables) patching

### POC of Computer in-game (DONE)

- [x] Make computer bigcraftable interactable on use action
- [x] Make computer interaction display centered window
    - [x] Make drawable window
    - [x] Make a set of custom draw primitives commands
    - [x] Make draw stack with merging last drawn image to one
    - [x] Make displayed window render custom graphics in batch using commands
    - [x] Make an abstraction over stardew's code
- [x] Add support for interpretable language in-library
- [x] Add VMs (sandboxes) for each computer instance in world
- [x] Add save state mechanism for computers, make them movable and attach id tag for any obtained computer
- [x] Add basic console interpreter for each computer
- [x] Add basic stdlib apis for computers
- [x] Add per-save state for computers

### Meaningful Computer (DONE)

- [x] Add support for file system (storage encapsulation)
- [x] Add support for custom packages loading
- [x] Add support for public networking
- [x] Add support for layered file system
- [x] Add support for separate core libraries
- [x] Add peripherals support with event-based communication
- [x] Add support for computer-to-computer communication
- [x] Add possibility of auto-discovering machine groups with computer as controller using peripheral

### More peripherals

- [x] Add CI with build + tests and tagged releases
- [x] Move subscribe/event-push handling into shared peripheral code
- [x] Add weather station peripheral
- [x] Add player sensor peripheral
- [ ] Add mailer peripheral
- [ ] Add shipping controller peripheral
- [ ] Add speaker peripheral
- [ ] Add switchable device peripherals
- [ ] Add monitor driven by computer over the network

### For future

- [ ] Add package-based libraries

## Kudos/Inspiration

- Automate mod for Stardew
- OpenComputers mod for Minecraft
- Graphics from Stardew Valley (original computer design recycled)

## References/Used resources

- HomeVideo CC0 font from https://ggbot.itch.io/home-video-font
- Monogram CC0 font from https://datagoblin.itch.io/monogram
- Jint library from https://github.com/sebastienros/jint
- Stardew Valley modding guide from https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started

## On LLMs

This mod **does** use LLM-assisted tools (i.e. Claude code with local models) for coding. Every artifact produced by an
LLM is carefully reviewed and iterated upon many times. None of other assets are generated, including images and text,
unless specified.

The technology itself should empower users, and bring our world to a better place than it is right now. In some capacity
it already does automate a lot of boilerplate and manual work, while allowing one to iterate on ideas faster. I truly
hope that one day the bubble will pop, and malicious actors in power would bear the consequences of their choices.

Please read: https://humanstatement.org/
