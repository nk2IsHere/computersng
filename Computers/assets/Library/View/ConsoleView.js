import { Keys } from "../Core/Constants"

export class ConsoleView {
    constructor(
        console,
        renderOptions,
        inputOptions,
        initialCommandContext,
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
        this.currentCommandContext = {
            ...initialCommandContext
        }
        
        this.inputHistory = []
        this.inputHistoryCursor = null
        
        this.currentInputOffset = 0
    }

    Render() {
        const { x, y, width, height, fontSize, fontCharacterWidth, fontCharacterHeight, textColor, backgroundColor } = this.renderOptions
        const { allowInput, inputPrefix } = this.inputOptions

        Render.Rectangle(x, y, width, height, backgroundColor)

        const maxLines = Math.floor(height / fontCharacterHeight)
        const maxCharactersPerLine = Math.floor(width / fontCharacterWidth)

        // Split the logs into lines by width
        const logs = this.console.Logs()
        const lines = []

        for (const log of logs) {
            for (let i = 0; i < log.message.length; i += maxCharactersPerLine) {
                lines.push(log.message.slice(i, i + maxCharactersPerLine))
            }
        }

        // Render the lines by offset
        let currentY = y

        // Choose top lines to render based on scrollOffset
        const minScrollOffset = -Math.max(0, lines.length - maxLines + (allowInput ? 1 : 0)) // input line takes 1 line
        const maxScrollOffset = 0
        const scrollOffset = Math.min(maxScrollOffset, Math.max(minScrollOffset, this.scrollOffset))

        // Reset scrollOffset if it's out of bounds
        if (this.scrollOffset !== scrollOffset) {
            this.scrollOffset = scrollOffset
        }

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
            const currentInputState = this.currentCommandContext.inputState?.toString() ?? ""
            const currentInputSliceStart = Math.max(
                0,
                this.currentInput.length - maxCharactersPerLine + inputPrefix.length + currentInputState.length
            )
            const currentInputSliceEnd = this.currentInput.length
            const currentInput = `${currentInputState}${inputPrefix}${this.currentInput.slice(currentInputSliceStart, currentInputSliceEnd)}`

            Render.Text(x, currentY, currentInput, fontSize, textColor)
            
            // Render the cursor
            const cursorX = x + (currentInput.length - this.currentInputOffset) * fontCharacterWidth
            const cursorY = currentY
            const cursorWidth = 1
            Render.Rectangle(cursorX, cursorY, cursorWidth, fontCharacterHeight, textColor)
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
                this.inputHistoryCursor = null
                
                const characterToInsert = currentlyHeldKeys.includes(Keys.fromName("LeftShift"))
                    ? key.upperCase
                    : key.lowerCase;
                
                const currentInputCursor = Math.max(0, this.currentInput.length - this.currentInputOffset)
                this.currentInput = this.currentInput.slice(0, currentInputCursor) + characterToInsert + this.currentInput.slice(currentInputCursor)
            }

            if (key.name === "Back") {
                const currentInputCursor = Math.max(0, this.currentInput.length - this.currentInputOffset)
                this.currentInput = this.currentInput.slice(0, currentInputCursor - 1) + this.currentInput.slice(currentInputCursor)
            }
            
            if (key.name === "Left") {
                this.currentInputOffset = Math.min(this.currentInputOffset + 1, this.currentInput.length)
            }
            
            if (key.name === "Right") {
                this.currentInputOffset = Math.max(this.currentInputOffset - 1, 0)
            }
            
            if (key.name === "Up") {
                if (this.inputHistoryCursor === null) {
                    this.inputHistoryCursor = -1 // It will be incremented to 0
                }
                
                const maxInputHistoryCursor = Math.max(0, this.inputHistory.length - 1)
                this.inputHistoryCursor = Math.min(this.inputHistoryCursor + 1, maxInputHistoryCursor)
                this.currentInput = this.inputHistory[this.inputHistoryCursor] ?? ""
            }
            
            if (key.name === "Down") {
                if (this.inputHistoryCursor === null) {
                    this.inputHistoryCursor = 0
                }

                this.inputHistoryCursor = Math.max(this.inputHistoryCursor - 1, 0)
                this.currentInput = this.inputHistory[this.inputHistoryCursor] ?? ""
            }

            if (key.name === "Enter") {
                const currentInputState = this.currentCommandContext.inputState?.toString() ?? ""
                const { inputPrefix } = this.inputOptions
                this.console.Info(`${currentInputState}${inputPrefix}${this.currentInput}`)

                this.onInput(this.currentInput, this.currentCommandContext)
                    .then((resultContext) => {
                        this.currentCommandContext = resultContext
                    })
                    .catch((error) => {
                        this.console.Error(error.message)
                    })

                this.inputHistory.unshift(this.currentInput)
                
                this.scrollOffset = 0 // scroll to bottom
                this.currentInput = ""
                this.currentInputOffset = 0
                this.inputHistoryCursor = null
            }
        }
    }
}
