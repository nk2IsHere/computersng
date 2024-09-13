
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
    
    Clear() {
        this.logs = []
    }
    
    Info(message) {
        message
            .split("\n")
            .forEach((line) => {
                this.Log(line, ConsoleLogLevel.Info)
            })
    }
    
    Warning(message) {
        message
            .split("\n")
            .forEach((line) => {
                this.Log(line, ConsoleLogLevel.Warning)
            })
    }
    
    Error(message) {
        message
            .split("\n")
            .forEach((line) => {
                this.Log(line, ConsoleLogLevel.Error)
            })
    }
    
    Logs() {
        return this.logs
    }
}
