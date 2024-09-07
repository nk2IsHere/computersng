
const facts = [
    "Memento Mori",
    "Memento Vivere",
    "Console uses JS for scripting",
    "Console by default executes JS statements, while terminal-like commands start with a dot",
    "It is possible to pipe a command to another command in the terminal, like this: `.cat a | .write b`",
    "Promises are not supported",
    "The console sandbox is not secure, do not run untrusted code",
    "More facts will be added in the future"
]

export function ChooseRandomFact() {
    const index = Math.floor(Math.random() * facts.length)
    return facts[index]
}
