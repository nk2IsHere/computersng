import { Color } from "./Core/Constants.js"
import { Console, ConsoleLogLevel, ConsoleView } from "./Core/Console.js"
import { ReloadView } from "./Core/Reload.js"
import { Exists, JoinPaths, List, ReadString } from "./Core/Storage.js"

export function Main() {
    const [screenWidth, screenHeight] = Render.GetScreenBoundaries()
    const defaultFontSize = Render.GetDefaultFontSize()
    const [fontCharacterWidth, fontCharacterHeight] = Render.MeasureGlyphSize('A', defaultFontSize)
    
    const console = new Console({
        logHistory: 100,
        defaultLogLevel: ConsoleLogLevel.Info
    })
    
    const consoleContext = {
        currentDirectory: "/"
    }
    
    const commands = {
        "help": {
            description: "Prints this help message",
            action: (args) => {
                console.Info("Available commands:")
                for (const [command, { description }] of Object.entries(commands)) {
                    console.Info(`-> .${command}: ${description}`)
                }
            }
        },
        "clear": {
            description: "Clear console",
            action: (args) => {
                console.Clear()
            }
        },
        "cd" : {
            description: "Change directory",
            action: (args) => {
                if (args.length === 0) {
                    console.Error("Usage: .cd <directory>")
                    return
                }
                const newDirectory = args[0]
                const newPath = JoinPaths(consoleContext.currentDirectory, newDirectory)
                if (!Exists(newPath)) {
                    console.Error(`Directory ${newPath} does not exist`)
                    return
                }
                
                consoleContext.currentDirectory = newPath
            }
        },
        "pwd": {
            description: "Print working directory",
            action: (args) => {
                console.Info(consoleContext.currentDirectory)
            }
        },
        "ls": {
            description: "List files in current directory",
            action: (args) => {
                const files = List(consoleContext.currentDirectory)
                if (files.length === 0) {
                    console.Info("No files in directory")
                    return
                }
                
                for (const { name, type, size } of files) {
                    console.Info(`${name} (${type}) - ${size} bytes`)
                }
            }
        },
        "cat": {
            description: "Print file content",
            action: (args) => {
                if (args.length === 0) {
                    console.Error("Usage: .cat <file>")
                    return
                }
                const file = args[0]
                const content = ReadString(JoinPaths(consoleContext.currentDirectory, file))
                console.Info(content)
            }
        }
    }

    function evaluateJsCommand(console, input) {
        try {
            const result = eval(input)
            const resultString = `${input} := ${result}`
            const resultByLine = resultString.split('\n')
            for (const line of resultByLine) {
                console.Info(line)
            }
        } catch (e) {
            const errorString = `${input} := ${e}`
            const errorByLine = errorString.split('\n')
            for (const line of errorByLine) {
                console.Error(line)
            }
        }
    }
    
    function evaluateCommand(console, input) {
        const [command, ...args] = input.split(' ')
        const commandName = command.slice(1)
        if (commands[commandName]) {
            try {
                commands[commandName].action(args)
            } catch (e) {
                console.Error(`Error while executing command: ${e}`)
            }
        } else {
            console.Error(`Unknown command: ${commandName}`)
        }
    }
    
    const consoleView = new ConsoleView(
        console,
        {
            x: 0,
            y: 0,
            width: screenWidth,
            height: screenHeight,
            fontSize: defaultFontSize,
            fontCharacterWidth: fontCharacterWidth,
            fontCharacterHeight: fontCharacterHeight,
            textColor: Color.while,
            backgroundColor: Color.black
        },
        {
            allowInput: true,
            inputMaxLength: 256,
            inputPrefix: "> "
        },
        (input) => {
            if(input.startsWith(".")) {
                evaluateCommand(console, input)
                consoleView.SetInputPrefixData(consoleContext.currentDirectory)
            } else {
                evaluateJsCommand(console, input)
            }
        }
    )
    
    const reloadView = new ReloadView(
        () => {
            throw new Error("Reload!")
        }
    )
    
    console.Info("Hello from console!")
    console.Info("Type .help to see available commands")
    while (true) {
        const latestEvents = Event.Poll()
        for (const event of latestEvents) {
            reloadView.Fire(event)
            consoleView.Fire(event)
        }
        
        Render.Begin()
        Render.Rectangle(0, 0, screenWidth, screenHeight, Color.black)
        consoleView.Render()
        reloadView.Render()
        Render.End()
    }
}
