import { Color } from "./Core/Constants.js"
import { Console, ConsoleLogLevel, ConsoleView } from "./Core/Console.js"
import { ReloadView } from "./Core/Reload.js"

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
        (input) => {
            try {
                const result = eval(input)
                const resultString = `${input} = ${result}`
                const resultByLine = resultString.split('\n')
                for (const line of resultByLine) {
                    console.Info(line)
                }
            } catch (e) {
                const errorString = `${input} = ${e}`
                const errorByLine = errorString.split('\n')
                for (const line of errorByLine) {
                    console.Error(line)
                }
            }
        }
    )
    
    const reloadView = new ReloadView(
        () => {
            throw new Error("Reload!")
        }
    )
    
    console.Info("Hello from console!")
    while (true) {
        const latestEvents = Event.Poll()
        for (const event of latestEvents) {
            reloadView.Fire(event)
            consoleView.Fire(event)
        }
        
        Render.Begin()
        Render.Rectangle(0, 0, screenWidth, screenHeight, Color.black)
        consoleView.Render()
        Render.End()
    }
}
