import { List } from "./Storage"
import { CommandResult } from "./Utils/Command"
import { Cache } from "./Utils/Cache"

export async function EvaluateJsCommand(console, context, input) {
    try {
        const result = await eval(input)
        const resultString = `${input} := ${result}`
        const resultByLine = resultString.split('\n')
        for (const line of resultByLine) {
            console.Info(line)
        }

        return CommandResult(context, result)
    } catch (e) {
        const errorString = `${input} := ${e}`
        const errorByLine = errorString.split('\n')
        for (const line of errorByLine) {
            console.Error(line)
        }

        return CommandResult(context, null, false)
    }
}

const consoleCommandsCached = new Cache({
    expireIn: 1000, // 1 second
    valueProvider: ({ console }) => List("/Command")
        .map(({ name }) => `/Command/${name}`)
        .map(path => {
            try {
                return System.LoadModule(path)
            } catch (e) {
                console.Error(`Error while loading module ${path}: ${e}`)
                return {}
            }
        })
        .flatMap(Object.values)
})

export async function EvaluateCommand(console, context, input) {
    const consoleCommands = consoleCommandsCached.ProvideValue({ console })
    
    const inputCommands = groupTokensByCommandAndArguments(parseTokensForInput(input))
        .filter(([command]) => command?.startsWith(".") === true) // Only allow commands starting with .
        .map(commandGroup => {
            const [command, ...args] = commandGroup
            return {
                inputCommand: command.slice(1),
                args: args ?? []
            }
        })

    try {
        let lastContext = {
            ...context,
            consoleCommands: consoleCommands,
            currentDirectory: context.currentDirectory ?? context.inputState,
            lastCommandResult: null,
            lastCommandSuccessful: true
        }
        
        for (const { inputCommand, args } of inputCommands) {
            const command = consoleCommands.find(({ command }) => inputCommand === command)
            if (command === undefined) {
                console.Error(`Command ${inputCommand} not found`)
                break
            }

            lastContext = await command.action(args, console, lastContext)

            if (!lastContext.lastCommandSuccessful) {
                break
            }
        }

        return {
            ...lastContext,
            inputState: lastContext.currentDirectory ?? lastContext.inputState
        }
    } catch (e) {
        console.Error(`Error while executing command: ${e}`)
        return {
            ...context,
            lastCommandResult: null,
            lastCommandSuccessful: false,
            inputState: context.currentDirectory ?? context.inputState
        }
    }
}

function parseTokensForInput(input) {
    // Example: .cd /a | .ls | .write file.txt
    //        : .write file.txt "Hello, World!"

    const tokens = []
    let currentToken = null
    let inString = false
    let escaped = false

    for (let i = 0; i < input.length; i++) {
        const char = input[i]

        if (char === " " && !inString) {
            if (currentToken !== null) {
                tokens.push(currentToken)
                currentToken = null
            }

            continue
        }

        if (char === "|" && !inString) {
            if (currentToken !== null) {
                tokens.push(currentToken)
                currentToken = null
            }
            tokens.push("|")

            continue
        }

        if (char === "\\" && inString && !escaped) {
            escaped = true
            continue
        }

        if (char === "\"" && !escaped) {
            inString = !inString
            continue
        }

        if (currentToken === null) {
            currentToken = ""
        }

        if (escaped) {
            escaped = false
        }

        currentToken += char
    }

    if (currentToken !== null) {
        tokens.push(currentToken)
    }

    return tokens
}

function groupTokensByCommandAndArguments(tokens) {
    const commandGroups = []
    let currentCommandGroup = []

    for (const token of tokens) {
        if (token === "|") {
            commandGroups.push(currentCommandGroup)
            currentCommandGroup = []
            continue
        }

        currentCommandGroup.push(token)
    }

    if (currentCommandGroup.length > 0) {
        commandGroups.push(currentCommandGroup)
    }

    return commandGroups
}
