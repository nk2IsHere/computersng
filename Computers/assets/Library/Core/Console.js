
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

        // Bumped on every change; lets views cache derived render data (e.g. wrapped lines).
        this.revision = 0
    }

    Log(message, level = this.defaultLogLevel) {
        const lines = (message ?? "").split("\n")
        for (const line of lines) {
            this.logs.push({ message: line, level })
            if (this.logs.length > this.logHistory) {
                this.logs.shift()
            }
        }
        this.revision++
    }

    Clear() {
        this.logs = []
        this.revision++
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
