import { Color } from "./Core/Constants.js"
import { Console, ConsoleLogLevel, ConsoleView } from "./Core/Console.js"
import { ReloadView } from "./Core/Reload.js"
import { EvaluateCommand, EvaluateJsCommand } from "./Core/Commands.js"
import { ChooseRandomFact } from "./Core/Facts.js"

export function Main() {
    const [screenWidth, screenHeight] = Render.GetScreenBoundaries()
    const defaultFontSize = Render.GetDefaultFontSize()
    const [fontCharacterWidth, fontCharacterHeight] = Render.MeasureGlyphSize('A', defaultFontSize)
    
    const console = new Console({
        logHistory: 100,
        defaultLogLevel: ConsoleLogLevel.Info
    })
    
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
        {
            inputState: "/"
        },
        async (input, context) => {
            const resultContext = await (
                input.startsWith('.') 
                    ? EvaluateCommand(console, context, input)
                    : EvaluateJsCommand(console, context, input)
            )
            
            return {
                ...context,
                ...resultContext
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
    console.Info(`Random fact of the day: ${ChooseRandomFact()}`)
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
