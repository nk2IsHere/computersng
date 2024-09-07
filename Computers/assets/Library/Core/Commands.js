import { Delete, Exists, JoinPaths, List, ReadString, WriteString } from "./Storage.js"

function commandResult(
    context,
    result = null,
    isSuccessful = true
) {
    const resultList = []
    if (result !== null && Array.isArray(result)) {
        resultList.push(...result)
    }

    if (result !== null && !Array.isArray(result)) {
        resultList.push(result)
    }

    return {
        ...context,
        lastCommandResult: resultList,
        lastCommandSuccessful: isSuccessful
    }
}

function commandResultWithContext(
    context,
    result = null,
    additionalContext = {},
    isSuccessful = true
) {
    return commandResult(
        {
            ...context,
            ...additionalContext
        },
        result,
        isSuccessful
    )
}

function commandArguments(context, args) {
    const lastCommandResult = context.lastCommandResult ?? []

    const argumentList = []
    const maxArguments = Math.max(args.length, lastCommandResult.length)
    for (let i = 0; i < maxArguments; i++) {
        argumentList.push(args[i] ?? lastCommandResult[i])
    }

    return argumentList
}

const ConsoleCommands = Object.freeze({
    "help": {
        description: "Prints this help message",
        action: (args, console, context) => {
            console.Info("Available commands:")
            for (const [command, {description}] of Object.entries(ConsoleCommands)) {
                console.Info(`-> .${command}: ${description}`)
            }

            return commandResult(context, null)
        }
    },
    "clear": {
        description: "Clear console",
        action: (args, console, context) => {
            console.Clear()
            return commandResult(context)
        }
    },
    "cd": {
        description: "Change directory",
        action: (args, console, context) => {
            const [changeDirectory] = commandArguments(context, args)

            if (!changeDirectory) {
                console.Error("Usage: .cd <directory>")
                return commandResult(context, null, false)
            }

            const newPath = JoinPaths(context.currentDirectory, changeDirectory)
            if (!Exists(newPath)) {
                console.Error(`Directory ${newPath} does not exist`)
                return commandResult(context, null, false)
            }

            return commandResultWithContext(context, newPath, { currentDirectory: newPath })
        }
    },
    "pwd": {
        description: "Print working directory",
        action: (args, console, context) => {
            console.Info(context.currentDirectory)
            return commandResult(context, context.currentDirectory)
        }
    },
    "ls": {
        description: "List files in current directory",
        action: (args, console, context) => {
            const [directory] = commandArguments(context, args)
            const directoryToList = directory ?? context.currentDirectory

            if (!directoryToList) {
                console.Error("Usage: .ls <directory>")
                return commandResult(context, null, false)
            }

            const files = List(directoryToList)
            if (files.length === 0) {
                console.Info("")
                return commandResult(context, [])
            }

            for (const { name, type, size } of files) {
                console.Info(`${name} (${type}) - ${size} bytes`)
            }

            return commandResult(context, files)
        }
    },
    "cat": {
        description: "Print file content",
        action: (args, console, context) => {
            const [file] = commandArguments(context, args)

            if (!file) {
                console.Error("Usage: .cat <file>")
                return
            }

            const content = ReadString(JoinPaths(context.currentDirectory, file))

            console.Info(content)
            return commandResult(context, content)
        }
    },
    "rm": {
        description: "Remove file",
        action: (args, console, context) => {
            const parsedArguments = commandArguments(context, args)
            const file = parsedArguments.pop()

            if (!file) {
                console.Error("Usage: .rm [--recursive] <file>")
                return commandResult(context, null, false)
            }

            const recursive = parsedArguments.includes("--recursive")
            const path = JoinPaths(context.currentDirectory, file)
            Delete(path, recursive)

            return commandResult(context)
        }
    },
    "write": {
        description: "Write content to file",
        action: (args, console, context) => {
            const [file, content] = commandArguments(context, args)

            if (!file || !content) {
                console.Error("Usage: .write <file> <content>")
                return commandResult(context, null, false)
            }

            const path = JoinPaths(context.currentDirectory, file)
            WriteString(path, content)
            return commandResult(context)
        }
    },
})

export function EvaluateJsCommand(console, context, input) {
    try {
        const result = eval(input)
        const resultString = `${input} := ${result}`
        const resultByLine = resultString.split('\n')
        for (const line of resultByLine) {
            console.Info(line)
        }

        return commandResult(context, result)
    } catch (e) {
        const errorString = `${input} := ${e}`
        const errorByLine = errorString.split('\n')
        for (const line of errorByLine) {
            console.Error(line)
        }

        return commandResult(context, null, false)
    }
}

export function EvaluateCommand(console, context, input) {
    const commands = input
        .split('|')
        .map(command => command.trim())
        .filter(command => command.length > 0)
        .filter(command => command[0] === '.') // Only allow commands starting with .
        .map(commandPart => {
            const [command, ...args] = commandPart.split(' ')
            return {
                command: command.slice(1),
                args: args ?? []
            }
        })

    try {
        let lastContext = {
            ...context,
            currentDirectory: context.currentDirectory ?? context.inputState,
            lastCommandResult: null,
            lastCommandSuccessful: true
        }
        for (const { command, args } of commands) {
            if (!ConsoleCommands[command]) {
                console.Error(`Command ${command} not found`)
                break
            }

            lastContext = ConsoleCommands[command].action(args, console, lastContext)

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
    }
}
