
const Colors = {
    Black: [0, 0, 0, 255],
    White: [255, 255, 255, 255],
    Red: [255, 0, 0, 255],
    Green: [0, 255, 0, 255],
    Blue: [0, 0, 255, 255],
    Yellow: [255, 255, 0, 255],
    Cyan: [0, 255, 255, 255],
    Magenta: [255, 0, 255, 255],
    Transparent: [0, 0, 0, 0]
}


const Entry = () => {
    const [screenWidth, screenHeight] = Render.GetScreenBoundaries()
    const defaultFontSize = Render.GetDefaultFontSize()
    
    while (true) {
        const latestEvents = Event.Poll()
        for (const event of latestEvents) {
            if (event.Type === "KeyPressed") {
                const [key] = event.Data
                if (key === 112) { // F1
                    throw new Error("Reload!")
                }
            }
        }
        
        Render.Begin()
        Render.Rectangle(0, 0, screenWidth, screenHeight, Colors.Black)
        Render.Text(0, 0, "Welcome from JS!", defaultFontSize, Colors.White)
        Render.End()
    }
}
