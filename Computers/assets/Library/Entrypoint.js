const { Colors } = System.Require("Core/Constants")
const { Keys } = System.Require("Core/Input")
const { Console, ConsoleLogLevel, ConsoleView } = System.Require("Core/Console")
const { ReloadView } = System.Require("Core/Reload")

const Entry = () => {
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
            textColor: Colors.White,
            backgroundColor: Colors.Black
        },
        {
            allowInput: true,
            inputMaxLength: 256,
            inputPrefix: "> "
        },
        (input) => {
            try {
                const result = eval(input)
                console.Info(`${input} = ${result}`)
            } catch (e) {
                console.Error(e.message)
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
        Render.Rectangle(0, 0, screenWidth, screenHeight, Colors.Black)
        consoleView.Render()
        Render.End()
    }
}
