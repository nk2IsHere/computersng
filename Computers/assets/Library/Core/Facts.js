
const facts = [
    "Memento Mori",
    "Memento Vivere",
    "Console uses Jint engine with ES6 support for scripting",
    "Console by default executes JS statements, while terminal-like commands start with a dot",
    "It is possible to pipe a command to another command in the terminal, like this: `.cat a | .write b`",
    "Promises are fully supported",
    "When writing your infinite loops, be sure to either poll events `Event.Poll()` or use `System.ProcessTasks()` to allow asynchronous tasks to resolve",
    "You can load any arbitrary module into the memory by using `System.LoadModule(path)`",
    "You can execute any arbitrary module from file by using `.exec` command. The module must export `async Main(console, context)` function",
    "Console commands are defined in the `Command` folder",
    "The console sandbox is not secure, do not run untrusted code",
    "Computer uses layered-style file system. There are 3 layers: Core (Read Only), External (Read Only), and Persistent (Read/Write)",
    "You can override any file in the system, or add new files by editing the External or Persistent layers",
    "External layer is read from Mods/Computers/storage/(your computer id) folder",
    "Persistent layer is in-memory and bound to the computer instance",
    "You can find the computer id in the console by typing `.id`",
    "You can change mod configuration by going to Mods/Computers/assets/Configuration.yml",
    "You can execute arbitrary script at startup by overriding the `Startup.js` file in the Persistent or External layer",
    "More facts will be added in the future"
]

export function ChooseRandomFact() {
    const index = Math.floor(Math.random() * facts.length)
    return facts[index]
}
