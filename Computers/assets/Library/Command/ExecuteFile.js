import { Exists, JoinPaths } from "../Core/Storage"
import { CommandArguments, CommandResult } from "../Core/Utils/Command"
import { EvaluateJsExecutable } from "../Core/Engine"

export default {
    command: "exec",
    description: "Execute script",
    usage: ".exec <script>",
    action: async (args, console, context) => {
        const [script] = CommandArguments(context, args)

        if (!script) {
            console.Error("Usage: .exec <script>")
            return CommandResult(context, null, false)
        }

        let scriptPath = JoinPaths(context.currentDirectory, script)
        if (!scriptPath.endsWith(".js")) {
            scriptPath += ".js"
        }

        const scriptExists = Exists(scriptPath)

        if (!scriptExists) {
            console.Error(`Script ${scriptPath} does not exist`)
            return CommandResult(context, null, false)
        }

        return EvaluateJsExecutable(console, context, scriptPath)
    }
}
