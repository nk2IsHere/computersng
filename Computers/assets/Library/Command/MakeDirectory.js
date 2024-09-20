import { Exists, JoinPaths, MakeDirectory } from "../Core/Storage"
import { CommandArguments, CommandResult } from "../Core/Utils/Command"

export default {
    command: "mkdir",
    description: "Create directory",
    usage: ".mkdir <directory>",
    action: async (args, console, context) => {
        const [directory] = CommandArguments(context, args)

        if (!directory) {
            console.Error("Usage: .mkdir <directory>")
            return CommandResult(context, null, false)
        }

        const path = JoinPaths(context.currentDirectory, directory)
        if (Exists(path)) {
            console.Error(`Directory ${path} already exists`)
            return CommandResult(context, null, false)
        }

        MakeDirectory(path)
        console.Info(`Created directory ${path}`)

        return CommandResult(context)
    }
}
