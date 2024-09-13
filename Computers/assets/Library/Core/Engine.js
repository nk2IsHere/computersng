import { CommandResult } from "./Utils"

export async function EvaluateJsExecutable(console, context, path) {
    try {
        const module = System.LoadModule(path)
        const result = await module.Main(console, context)

        return CommandResult(context, result)
    } catch (e) {
        console.Error(`Error while executing script: ${e}`)
        return CommandResult(context, null, false)
    }
}
