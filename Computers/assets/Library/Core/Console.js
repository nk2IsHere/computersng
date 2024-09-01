import { Keys } from "./Constants.js"

export const ConsoleLogLevel = Object.freeze({
    Info: "info",
    Warning: "warning",
    Error: "error"
})

export class Console {
    constructor({
        logHistory = 100,
        defaultLogLevel = ConsoleLogLevel.Info
    }) {
        this.logs = []
        this.logHistory = logHistory
        this.defaultLogLevel = defaultLogLevel
    }
    
    Log(message, level = this.defaultLogLevel) {
        this.logs.push({
            message,
            level
        })
        
        if (this.logs.length > this.logHistory) {
            this.logs.shift()
        }
    }
    
    Info(message) {
        this.Log(message, ConsoleLogLevel.Info)
    }
    
    Warning(message) {
        this.Log(message, ConsoleLogLevel.Warning)
    }
    
    Error(message) {
        this.Log(message, ConsoleLogLevel.Error)
    }
    
    Logs() {
        return this.logs
    }
}

export class ConsoleView {
    constructor(
        console,
        renderOptions,
        inputOptions,
        onInput
    ) {
        this.console = console
        
        const { x, y, width, height, fontSize, fontCharacterWidth, fontCharacterHeight, textColor, backgroundColor } = renderOptions
        this.renderOptions = { x, y, width, height, fontSize, fontCharacterWidth, fontCharacterHeight, textColor, backgroundColor }
        
        const { allowInput, inputMaxLength, inputPrefix } = inputOptions
        this.inputOptions = { allowInput, inputMaxLength, inputPrefix }
        this.onInput = onInput
        
        // negative scrollOffset means scroll up
        // positive scrollOffset means scroll down
        this.scrollOffset = 0
        
        this.currentInput = ""
    }
    
    Render() {
        const { x, y, width, height, fontSize, fontCharacterWidth, fontCharacterHeight, textColor, backgroundColor } = this.renderOptions
        const { allowInput, inputPrefix } = this.inputOptions
        
        Render.Rectangle(x, y, width, height, backgroundColor)
        
        const maxLines = Math.floor(height / fontCharacterHeight)
        const maxCharactersPerLine = Math.floor(width / fontCharacterWidth)
        
        // Take last `maxLines` lines from the console offset by `scrollOffset`
        const logs = this.console.Logs().slice(-maxLines + this.scrollOffset)
        
        // Split the logs into lines by width
        const lines = []
        
        for (const log of logs) {
            for (let i = 0; i < log.message.length; i += maxCharactersPerLine) {
                lines.push(log.message.slice(i, i + maxCharactersPerLine))
            }
        }
        
        // Render the lines by offset
        let currentY = y
        
        // Choose top lines to render based on scrollOffset
        const minScrollOffset = 0
        const maxScrollOffset = Math.max(0, lines.length - maxLines)
        const scrollOffset = Math.min(maxScrollOffset, Math.max(minScrollOffset, this.scrollOffset))
        
        const linesCount = Math.floor(maxLines) - (allowInput ? 1 : 0) // input line takes 1 line
        const linesStart = lines.length - linesCount + scrollOffset
        const linesEnd = lines.length + scrollOffset
        
        for (let i = linesStart; i < linesEnd; i++) {
            const line = lines[i]
            if (line === undefined) {
                continue
            }
            
            Render.Text(x, currentY, line, fontSize, textColor)
            currentY += fontCharacterHeight
        }
        
        // Render the input line
        if (allowInput) {
            const currentInputSliceStart = Math.max(0, this.currentInput.length - maxCharactersPerLine + inputPrefix.length)
            const currentInputSliceEnd = this.currentInput.length
            const currentInput = inputPrefix + this.currentInput.slice(currentInputSliceStart, currentInputSliceEnd)
            
            Render.Text(x, currentY, currentInput, fontSize, textColor)
        }
    }
    
    Fire(event) {
        if(event.Type === "MouseWheel") {
            const [delta] = event.Data
            const deltaScaled = Math.sign(delta) * Math.ceil(Math.abs(delta) / 1000)
            this.scrollOffset += deltaScaled
        }
        
        if(event.Type === "KeyPressed" && this.inputOptions.allowInput) {
            const [keyRaw, currentlyHeldKeysRaw] = event.Data
            const key = Keys[keyRaw]
            const currentlyHeldKeys = currentlyHeldKeysRaw.map((keyRaw) => Keys[keyRaw])
            
            if (!key.isSpecial) {
                this.currentInput += (
                    currentlyHeldKeys.includes(Keys[160])
                        ? key.upperCase
                        : key.lowerCase
                )
            }
            
            if (key.name === "Back") {
                this.currentInput = this.currentInput.slice(0, -1)
            }
            
            if (key.name === "Enter") {
                this.onInput(this.currentInput)
                this.currentInput = ""
            }
        }
    }
}
