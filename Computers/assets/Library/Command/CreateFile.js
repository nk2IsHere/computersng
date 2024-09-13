import { Exists, JoinPaths, WriteString } from "../Core/Storage"
import { CommandArguments, CommandResult } from "../Core/Utils"

export default {
    command: "touch",
    description: "Create file",
    usage: ".touch <file>",
    action: async (args, console, context) => {
        const [file] = CommandArguments(context, args)

        if (!file) {
            console.Error("Usage: .touch <file>")
            return CommandResult(context, null, false)
        }

        const path = JoinPaths(context.currentDirectory, file)
        if (Exists(path)) {
            console.Error(`File ${path} already exists`)
            return CommandResult(context, null, false)
        }

        WriteString(path, "")
        console.Info(`Created file ${path}`)

        return CommandResult(context)
    }
}
