import { Delete, JoinPaths } from "../Core/Storage"
import { CommandArguments, CommandResult } from "../Core/Utils"

export default {
    command: "rm",
    description: "Remove file",
    usage: ".rm [--recursive] <file>",
    action: async (args, console, context) => {
        const parsedArguments = CommandArguments(context, args)
        const file = parsedArguments.pop()

        if (!file) {
            console.Error("Usage: .rm [--recursive] <file>")
            return CommandResult(context, null, false)
        }

        const recursive = parsedArguments.includes("--recursive")
        const path = JoinPaths(context.currentDirectory, file)
        Delete(path, { recursive })

        return CommandResult(context)
    }
}
