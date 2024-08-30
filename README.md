# Computers

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
    - auto-discovers machines indirectly connected to it (meaning that machines may form a group by attaching to each other and computer)
    - has peripherals


- Add monitor bigcraftable (can be placed anywhere) which
    - will have its own api to display requested data
    - will be of different sizes
    - will have to be connected directly to computer to operate


- Add peripherals:
    - Wireless stations - allows communication with other computers on a limited distance
    - ??? (still undecided)

## TODOs

### Basic item support

- [x] Add bigcraftable type
- [x] Add craftingrecipe type
- [x] Add support for functional redux-like store
- [x] Create dataStore for patcher
- [x] Add support for figuring out the next ids for mod's bigcraftables
- [x] Add dictionary (Data/BigCraftablesInformation, Data/CraftingRecipes) patching
- [x] Add tilesheet (TileSheets/Craftables) patching

### Basic POC of computer in-game (IN PROGRESS)

- [x] Make computer bigcraftable interactable on use action
- [x] Make computer interaction display centered window
    - [x] Make drawable window
    - [x] Make a set of custom draw primitives commands
    - [x] Make draw stack with merging last drawn image to one
    - [x] Make displayed window render custom graphics in batch using commands
    - [x] Make an abstraction over stardew's code
- [x] Add support for interpretable language in-library
- [ ] Add VMs (sandboxes) for each computer instance in world
- [ ] Add save state mechanism for computers, make them movable and attach id tag for any obtained computer
- [ ] Add basic console interpreter for each computer
- [ ] Add basic stdlib apis for computers

### Interactability with world (TODO)

- [ ] Add possibility of auto-discovering machine groups with computer as controller
- [ ] ???

## Kudos/Inspiration

- Automate mod for Stardew
- OpenComputers mod for Minecraft

## References/Used resources

- HomeVideo CC0 font from https://ggbot.itch.io/home-video-font
- MoonSharp library for C# from https://www.moonsharp.org
- Stardew Valley modding guide from https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started
