
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

const Keys = Object.freeze({
    0: {
        isSpecial: true,
        name: "None"
    },
    8: {
        isSpecial: true,
        name: "Back"
    },
    9: {
        isSpecial: true,
        name: "Tab"
    },
    13: {
        isSpecial: true,
        name: "Enter"
    },
    19: {
        isSpecial: true,
        name: "Pause"
    },
    20: {
        isSpecial: true,
        name: "CapsLock"
    },
    21: {
        isSpecial: true,
        name: "Kana"
    },
    25: {
        isSpecial: true,
        name: "Kanji"
    },
    27: {
        isSpecial: true,
        name: "Escape"
    },
    28: {
        isSpecial: true,
        name: "ImeConvert"
    },
    29: {
        isSpecial: true,
        name: "ImeNoConvert"
    },
    32: {
        isSpecial: true,
        name: "Space"
    },
    33: {
        isSpecial: true,
        name: "PageUp"
    },
    34: {
        isSpecial: true,
        name: "PageDown"
    },
    35: {
        isSpecial: true,
        name: "End"
    },
    36: {
        isSpecial: true,
        name: "Home"
    },
    37: {
        isSpecial: true,
        name: "Left"
    },
    38: {
        isSpecial: true,
        name: "Up"
    },
    39: {
        isSpecial: true,
        name: "Right"
    },
    40: {
        isSpecial: true,
        name: "Down"
    },
    41: {
        isSpecial: true,
        name: "Select"
    },
    42: {
        isSpecial: true,
        name: "Print"
    },
    43: {
        isSpecial: true,
        name: "Execute"
    },
    44: {
        isSpecial: true,
        name: "PrintScreen"
    },
    45: {
        isSpecial: true,
        name: "Insert"
    },
    46: {
        isSpecial: true,
        name: "Delete"
    },
    47: {
        isSpecial: true,
        name: "Help"
    },
    48: {
        isSpecial: false,
        name: "D0",
        lowerCase: "0",
        upperCase: ")"
    },
    49: {
        isSpecial: false,
        name: "D1",
        lowerCase: "1",
        upperCase: "!"
    },
    50: {
        isSpecial: false,
        name: "D2",
        lowerCase: "2",
        upperCase: "@"
    },
    51: {
        isSpecial: false,
        name: "D3",
        lowerCase: "3",
        upperCase: "#"
    },
    52: {
        isSpecial: false,
        name: "D4",
        lowerCase: "4",
        upperCase: "$"
    },
    53: {
        isSpecial: false,
        name: "D5",
        lowerCase: "5",
        upperCase: "%"
    },
    54: {
        isSpecial: false,
        name: "D6",
        lowerCase: "6",
        upperCase: "^"
    },
    55: {
        isSpecial: false,
        name: "D7",
        lowerCase: "7",
        upperCase: "&"
    },
    56: {
        isSpecial: false,
        name: "D8",
        lowerCase: "8",
        upperCase: "*"
    },
    57: {
        isSpecial: false,
        name: "D9",
        lowerCase: "9",
        upperCase: "("
    },
    65: {
        isSpecial: false,
        name: "A",
        lowerCase: "a",
        upperCase: "A"
    },
    66: {
        isSpecial: false,
        name: "B",
        lowerCase: "b",
        upperCase: "B"
    },
    67: {
        isSpecial: false,
        name: "C",
        lowerCase: "c",
        upperCase: "C"
    },
    68: {
        isSpecial: false,
        name: "D",
        lowerCase: "d",
        upperCase: "D"
    },
    69: {
        isSpecial: false,
        name: "E",
        lowerCase: "e",
        upperCase: "E"
    },
    70: {
        isSpecial: false,
        name: "F",
        lowerCase: "f",
        upperCase: "F"
    },
    71: {
        isSpecial: false,
        name: "G",
        lowerCase: "g",
        upperCase: "G"
    },
    72: {
        isSpecial: false,
        name: "H",
        lowerCase: "h",
        upperCase: "H"
    },
    73: {
        isSpecial: false,
        name: "I",
        lowerCase: "i",
        upperCase: "I"
    },
    74: {
        isSpecial: false,
        name: "J",
        lowerCase: "j",
        upperCase: "J"
    },
    75: {
        isSpecial: false,
        name: "K",
        lowerCase: "k",
        upperCase: "K"
    },
    76: {
        isSpecial: false,
        name: "L",
        lowerCase: "l",
        upperCase: "L"
    },
    77: {
        isSpecial: false,
        name: "M",
        lowerCase: "m",
        upperCase: "M"
    },
    78: {
        isSpecial: false,
        name: "N",
        lowerCase: "n",
        upperCase: "N"
    },
    79: {
        isSpecial: false,
        name: "O",
        lowerCase: "o",
        upperCase: "O"
    },
    80: {
        isSpecial: false,
        name: "P",
        lowerCase: "p",
        upperCase: "P"
    },
    81: {
        isSpecial: false,
        name: "Q",
        lowerCase: "q",
        upperCase: "Q"
    },
    82: {
        isSpecial: false,
        name: "R",
        lowerCase: "r",
        upperCase: "R"
    },
    83: {
        isSpecial: false,
        name: "S",
        lowerCase: "s",
        upperCase: "S"
    },
    84: {
        isSpecial: false,
        name: "T",
        lowerCase: "t",
        upperCase: "T"
    },
    85: {
        isSpecial: false,
        name: "U",
        lowerCase: "u",
        upperCase: "U"
    },
    86: {
        isSpecial: false,
        name: "V",
        lowerCase: "v",
        upperCase: "V"
    },
    87: {
        isSpecial: false,
        name: "W",
        lowerCase: "w",
        upperCase: "W"
    },
    88: {
        isSpecial: false,
        name: "X",
        lowerCase: "x",
        upperCase: "X"
    },
    89: {
        isSpecial: false,
        name: "Y",
        lowerCase: "y",
        upperCase: "Y"
    },
    90: {
        isSpecial: false,
        name: "Z",
        lowerCase: "z",
        upperCase: "Z"
    },
    91: {
        isSpecial: true,
        name: "LeftWindows"
    },
    92: {
        isSpecial: true,
        name: "RightWindows"
    },
    93: {
        isSpecial: true,
        name: "Apps"
    },
    95: {
        isSpecial: true,
        name: "Sleep"
    },
    96: {
        isSpecial: false,
        name: "NumPad0",
        lowerCase: "0",
        upperCase: "0"
    },
    97: {
        isSpecial: false,
        name: "NumPad1",
        lowerCase: "1",
        upperCase: "1"
    },
    98: {
        isSpecial: false,
        name: "NumPad2",
        lowerCase: "2",
        upperCase: "2"
    },
    99: {
        isSpecial: false,
        name: "NumPad3",
        lowerCase: "3",
        upperCase: "3"
    },
    100: {
        isSpecial: false,
        name: "NumPad4",
        lowerCase: "4",
        upperCase: "4"
    },
    101: {
        isSpecial: false,
        name: "NumPad5",
        lowerCase: "5",
        upperCase: "5"
    },
    102: {
        isSpecial: false,
        name: "NumPad6",
        lowerCase: "6",
        upperCase: "6"
    },
    103: {
        isSpecial: false,
        name: "NumPad7",
        lowerCase: "7",
        upperCase: "7"
    },
    104: {
        isSpecial: false,
        name: "NumPad8",
        lowerCase: "8",
        upperCase: "8"
    },
    105: {
        isSpecial: false,
        name: "NumPad9",
        lowerCase: "9",
        upperCase: "9"
    },
    106: {
        isSpecial: false,
        name: "Multiply",
        lowerCase: "*",
        upperCase: "*"
    },
    107: {
        isSpecial: false,
        name: "Add",
        lowerCase: "+",
        upperCase: "+"
    },
    108: {
        isSpecial: false,
        name: "Separator",
        lowerCase: "|",
        upperCase: "|"
    },
    109: {
        isSpecial: false,
        name: "Subtract",
        lowerCase: "-",
        upperCase: "-"
    },
    110: {
        isSpecial: false,
        name: "Decimal",
        lowerCase: ".",
        upperCase: "."
    },
    111: {
        isSpecial: false,
        name: "Divide",
        lowerCase: "/",
        upperCase: "/"
    },
    112: {
        isSpecial: true,
        name: "F1"
    },
    113: {
        isSpecial: true,
        name: "F2"
    },
    114: {
        isSpecial: true,
        name: "F3"
    },
    115: {
        isSpecial: true,
        name: "F4"
    },
    116: {
        isSpecial: true,
        name: "F5"
    },
    117: {
        isSpecial: true,
        name: "F6"
    },
    118: {
        isSpecial: true,
        name: "F7"
    },
    119: {
        isSpecial: true,
        name: "F8"
    },
    120: {
        isSpecial: true,
        name: "F9"
    },
    121: {
        isSpecial: true,
        name: "F10"
    },
    122: {
        isSpecial: true,
        name: "F11"
    },
    123: {
        isSpecial: true,
        name: "F12"
    },
    124: {
        isSpecial: true,
        name: "F13"
    },
    125: {
        isSpecial: true,
        name: "F14"
    },
    126: {
        isSpecial: true,
        name: "F15"
    },
    127: {
        isSpecial: true,
        name: "F16"
    },
    128: {
        isSpecial: true,
        name: "F17"
    },
    129: {
        isSpecial: true,
        name: "F18"
    },
    130: {
        isSpecial: true,
        name: "F19"
    },
    131: {
        isSpecial: true,
        name: "F20"
    },
    132: {
        isSpecial: true,
        name: "F21"
    },
    133: {
        isSpecial: true,
        name: "F22"
    },
    134: {
        isSpecial: true,
        name: "F23"
    },
    135: {
        isSpecial: true,
        name: "F24"
    },
    144: {
        isSpecial: true,
        name: "NumLock"
    },
    145: {
        isSpecial: true,
        name: "Scroll"
    },
    160: {
        isSpecial: true,
        name: "LeftShift"
    },
    161: {
        isSpecial: true,
        name: "RightShift"
    },
    162: {
        isSpecial: true,
        name: "LeftControl"
    },
    163: {
        isSpecial: true,
        name: "RightControl"
    },
    164: {
        isSpecial: true,
        name: "LeftAlt"
    },
    165: {
        isSpecial: true,
        name: "RightAlt"
    },
    166: {
        isSpecial: true,
        name: "BrowserBack"
    },
    167: {
        isSpecial: true,
        name: "BrowserForward"
    },
    168: {
        isSpecial: true,
        name: "BrowserRefresh"
    },
    169: {
        isSpecial: true,
        name: "BrowserStop"
    },
    170: {
        isSpecial: true,
        name: "BrowserSearch"
    },
    171: {
        isSpecial: true,
        name: "BrowserFavorites"
    },
    172: {
        isSpecial: true,
        name: "BrowserHome"
    },
    173: {
        isSpecial: true,
        name: "VolumeMute"
    },
    174: {
        isSpecial: true,
        name: "VolumeDown"
    },
    175: {
        isSpecial: true,
        name: "VolumeUp"
    },
    176: {
        isSpecial: true,
        name: "MediaNextTrack"
    },
    177: {
        isSpecial: true,
        name: "MediaPreviousTrack"
    },
    178: {
        isSpecial: true,
        name: "MediaStop"
    },
    179: {
        isSpecial: true,
        name: "MediaPlayPause"
    },
    180: {
        isSpecial: true,
        name: "LaunchMail"
    },
    181: {
        isSpecial: true,
        name: "SelectMedia"
    },
    182: {
        isSpecial: true,
        name: "LaunchApplication1"
    },
    183: {
        isSpecial: true,
        name: "LaunchApplication2"
    },
    186: {
        isSpecial: false,
        name: "OemSemicolon",
        lowerCase: ";",
        upperCase: ":"
    },
    187: {
        isSpecial: false,
        name: "OemPlus",
        lowerCase: "=",
        upperCase: "+"
    },
    188: {
        isSpecial: false,
        name: "OemComma",
        lowerCase: ",",
        upperCase: "<"
    },
    189: {
        isSpecial: false,
        name: "OemMinus",
        lowerCase: "-",
        upperCase: "_"
    },
    190: {
        isSpecial: false,
        name: "OemPeriod",
        lowerCase: ".",
        upperCase: ">"
    },
    191: {
        isSpecial: false,
        name: "OemQuestion",
        lowerCase: "/",
        upperCase: "?"
    },
    192: {
        isSpecial: false,
        name: "OemTilde",
        lowerCase: "`",
        upperCase: "~"
    },
    202: {
        isSpecial: true,
        name: "ChatPadGreen"
    },
    203: {
        isSpecial: true,
        name: "ChatPadOrange"
    },
    219: {
        isSpecial: false,
        name: "OemOpenBrackets",
        lowerCase: "[",
        upperCase: "{"
    },
    220: {
        isSpecial: false,
        name: "OemPipe",
        lowerCase: "\\",
        upperCase: "|"
    },
    221: {
        isSpecial: false,
        name: "OemCloseBrackets",
        lowerCase: "]",
        upperCase: "}"
    },
    222: {
        isSpecial: false,
        name: "OemQuotes",
        lowerCase: "'",
        upperCase: "\""
    },
    223: {
        isSpecial: false,
        name: "Oem8",
        lowerCase: "§",
        upperCase: "±"
    },
    226: {
        isSpecial: false,
        name: "OemBackslash",
        lowerCase: "\\",
        upperCase: "|"
    },
    229: {
        isSpecial: true,
        name: "ProcessKey"
    },
    242: {
        isSpecial: true,
        name: "OemCopy"
    },
    243: {
        isSpecial: true,
        name: "OemAuto"
    },
    244: {
        isSpecial: true,
        name: "OemEnlW"
    },
    246: {
        isSpecial: true,
        name: "Attn"
    },
    247: {
        isSpecial: true,
        name: "Crsel"
    },
    248: {
        isSpecial: true,
        name: "Exsel"
    },
    249: {
        isSpecial: true,
        name: "EraseEof"
    },
    250: {
        isSpecial: true,
        name: "Play"
    },
    251: {
        isSpecial: true,
        name: "Zoom"
    },
    253: {
        isSpecial: true,
        name: "Pa1"
    },
    254: {
        isSpecial: true,
        name: "OemClear"
    }
})


class ReloadView {
    constructor(onReload) {
        this.onReload = onReload
    }
    
    Fire(event) {
        if(event.Type === "KeyPressed") {
            const [key] = event.Data
            if (key === 112) { // F1
                this.onReload()
            }
        }
    }
}

const ConsoleLogLevel = Object.freeze({
    Info: "info",
    Warning: "warning",
    Error: "error"
})

class Console {
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

class ConsoleView {
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
