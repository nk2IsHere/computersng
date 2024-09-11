import {
    Copy,
    Delete,
    Exists,
    JoinPaths,
    List,
    MakeDirectory,
    ReadString,
    WriteString
} from "./Storage.js"
import {HttpRequestString} from "./Network";

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
    return [
        ...args,
        ...lastCommandResult
    ]
}

const ConsoleCommands = Object.freeze({
    "help": {
        description: "Prints this help message",
        usage: ".help",
        action: async (args, console, context) => {
            console.Info("Available commands:")
            for (const [command, { description, usage}] of Object.entries(ConsoleCommands)) {
                console.Info(`-> .${command}: ${description} (${usage})`)
            }

            return commandResult(context, null)
        }
    },
    "clear": {
        description: "Clear console",
        usage: ".clear",
        action: async (args, console, context) => {
            console.Clear()
            return commandResult(context)
        }
    },
    "cd": {
        description: "Change directory",
        usage: ".cd <directory>",
        action: async (args, console, context) => {
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
        usage: ".pwd",
        action: async (args, console, context) => {
            console.Info(context.currentDirectory)
            return commandResult(context, context.currentDirectory)
        }
    },
    "ls": {
        description: "List files in current directory",
        usage: ".ls <directory>",
        action: async (args, console, context) => {
            const [directory] = commandArguments(context, args)
            const directoryToList = directory ?? context.currentDirectory

            if (!directoryToList) {
                console.Error("Usage: .ls <directory>")
                return commandResult(context, null, false)
            }

            const files = List(directoryToList)
            if (files.length === 0) {
                console.Info(" ")
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
        usage: ".cat <file>",
        action: async (args, console, context) => {
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
        usage: ".rm [--recursive] <file>",
        action: async (args, console, context) => {
            const parsedArguments = commandArguments(context, args)
            const file = parsedArguments.pop()

            if (!file) {
                console.Error("Usage: .rm [--recursive] <file>")
                return commandResult(context, null, false)
            }

            const recursive = parsedArguments.includes("--recursive")
            const path = JoinPaths(context.currentDirectory, file)
            Delete(path, { recursive })

            return commandResult(context)
        }
    },
    "write": {
        description: "Write content to file",
        usage: ".write <file> <content>",
        action: async (args, console, context) => {
            const [file, content] = commandArguments(context, args)

            if (!file || !content) {
                console.Error("Usage: .write <file> <content>")
                return commandResult(context, null, false)
            }

            const path = JoinPaths(context.currentDirectory, file)
            WriteString(path, content)
            
            console.Info(`Wrote content to ${path}`)
            return commandResult(context)
        }
    },
    "exec": {
        description: "Execute script",
        usage: ".exec <script>",
        action: async (args, console, context) => {
            const [script] = commandArguments(context, args)

            if (!script) {
                console.Error("Usage: .exec <script>")
                return commandResult(context, null, false)
            }

            let scriptPath = JoinPaths(context.currentDirectory, script)
            if (!scriptPath.endsWith(".js")) {
                scriptPath += ".js"
            }
            
            const scriptExists = Exists(scriptPath)
            
            if (!scriptExists) {
                console.Error(`Script ${scriptPath} does not exist`)
                return commandResult(context, null, false)
            }

            return EvaluateJsExecutable(console, context, scriptPath)
        }
    },
    "echo": {
        description: "Print message",
        usage: ".echo <message>",
        action: async (args, console, context) => {
            const [message] = commandArguments(context, args)

            if (!message) {
                console.Error("Usage: .echo <message>")
                return commandResult(context, null, false)
            }

            console.Info(message)
            return commandResult(context, message)
        }
    },
    "mkdir": {
        description: "Create directory",
        usage: ".mkdir <directory>",
        action: async (args, console, context) => {
            const [directory] = commandArguments(context, args)

            if (!directory) {
                console.Error("Usage: .mkdir <directory>")
                return commandResult(context, null, false)
            }

            const path = JoinPaths(context.currentDirectory, directory)
            if (Exists(path)) {
                console.Error(`Directory ${path} already exists`)
                return commandResult(context, null, false)
            }

            MakeDirectory(path)
            console.Info(`Created directory ${path}`)
            
            return commandResult(context)
        }
    },
    "touch": {
        description: "Create file",
        usage: ".touch <file>",
        action: async (args, console, context) => {
            const [file] = commandArguments(context, args)

            if (!file) {
                console.Error("Usage: .touch <file>")
                return commandResult(context, null, false)
            }

            const path = JoinPaths(context.currentDirectory, file)
            if (Exists(path)) {
                console.Error(`File ${path} already exists`)
                return commandResult(context, null, false)
            }

            WriteString(path, "")
            console.Info(`Created file ${path}`)
            
            return commandResult(context)
        }
    },
    "cp": {
        description: "Copy file or directory",
        usage: ".cp [--recursive] [--overwrite] <source> <destination>",
        action: async (args, console, context) => {
            const parsedArguments = commandArguments(context, args)
            
            const recursive = parsedArguments.includes("--recursive")
            const overwrite = parsedArguments.includes("--overwrite")
            
            const source = parsedArguments[parsedArguments.length - 2]
            const destination = parsedArguments[parsedArguments.length - 1]
            
            if (!source || !destination) {
                console.Error("Usage: .cp [--recursive] [--overwrite] <source> <destination>")
                return commandResult(context, null, false)
            }

            Copy(source, destination, { move: false, recursive, overwrite })
            console.Info(`Copied ${source} to ${destination}`)
            
            return commandResult(context)
        }
    },
    "mv": {
        description: "Move file or directory",
        usage: ".mv [--recursive] [--overwrite] <source> <destination>",
        action: async (args, console, context) => {
            const parsedArguments = commandArguments(context, args)
            
            const recursive = parsedArguments.includes("--recursive")
            const overwrite = parsedArguments.includes("--overwrite")
            
            const source = parsedArguments[parsedArguments.length - 2]
            const destination = parsedArguments[parsedArguments.length - 1]
            
            if (!source || !destination) {
                console.Error("Usage: .mv [--overwrite] <source> <destination>")
                return commandResult(context, null, false)
            }

            Copy(source, destination, { move: true, overwrite, recursive })
            console.Info(`Moved ${source} to ${destination}`)
            
            return commandResult(context)
        }
    },
    "http": {
        description: "HTTP request",
        usage: ".http <method> <url> [--headers key1=value1 key2=value2] [--data <data>]",
        action: async (args, console, context) => {
            const parsedArguments = commandArguments(context, args)
            const method = parsedArguments[0]
            const url = parsedArguments[1]

            if (!method || !url) {
                console.Error("Usage: .http <method> <url> [--headers key1=value1 key2=value2] [--data <data>]")
                return commandResult(context, null, false)
            }

            const headers = {}
            let data = null

            for (let i = 2; i < parsedArguments.length; i++) {
                const arg = parsedArguments[i]

                if (arg === "--headers") {
                    const headerArgs = parsedArguments[i + 1]
                    for (const headerArg of headerArgs) {
                        const [key, value] = headerArg.split("=")
                        headers[key] = value
                    }

                    i++
                    continue
                }

                if (arg === "--data") {
                    data = parsedArguments[i + 1]
                    i++
                    continue
                }

                console.Error(`Unknown argument: ${arg}`)
            }

            const response = await HttpRequestString(url, method, data, headers)
            if (response.statusCode >= 400) {
                console.Error(`Request failed with status code ${response.statusCode}`)
                return commandResult(context, response.body, false)
            }
            
            console.Info(`Request successful with status code ${response.statusCode}`)
            return commandResult(context, response.body)
        }
    },
    "id": {
        description: "Print system identification",
        usage: ".id",
        action: async (args, console, context) => {
            console.Info(`System identification: ${System.Id()}`)
            return commandResult(context)
        }
    },
})

export async function EvaluateJsExecutable(console, context, path) {
    try {
        const module = System.LoadModule(path)
        const result = await module.Main(console, context)
        
        return commandResult(context, result)
    } catch (e) {
        console.Error(`Error while executing script: ${e}`)
        return commandResult(context, null, false)
    }
}

export async function EvaluateJsCommand(console, context, input) {
    try {
        const result = await eval(input)
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

export async function EvaluateCommand(console, context, input) {
    const commands = groupTokensByCommandAndArguments(parseTokensForInput(input))
        .filter(commandGroup => commandGroup[0].startsWith(".")) // Only allow commands starting with .
        .map(commandGroup => {
            const [command, ...args] = commandGroup
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

            lastContext = await ConsoleCommands[command].action(args, console, lastContext)

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
