import { Exists, JoinPaths } from "../Core/Storage"
import { CommandArguments, CommandResult, CommandResultWithContext } from "../Core/Utils"

export default {
    command: "cd",
    description: "Change directory",
    usage: ".cd <directory>",
    action: async (args, console, context) => {
        const [changeDirectory] = CommandArguments(context, args)

        if (!changeDirectory) {
            console.Error("Usage: .cd <directory>")
            return CommandResult(context, null, false)
        }

        const newPath = JoinPaths(context.currentDirectory, changeDirectory)
        if (!Exists(newPath)) {
            console.Error(`Directory ${newPath} does not exist`)
            return CommandResult(context, null, false)
        }

        return CommandResultWithContext(context, newPath, { currentDirectory: newPath })
    }
}
