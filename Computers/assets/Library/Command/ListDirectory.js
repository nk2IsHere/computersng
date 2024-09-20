import { List } from "../Core/Storage"
import { CommandArguments, CommandResult } from "../Core/Utils/Command"

export default {
    command: "ls",
    description: "List files in current directory",
    usage: ".ls <directory>",
    action: async (args, console, context) => {
        const [directory] = CommandArguments(context, args)
        const directoryToList = directory ?? context.currentDirectory

        if (!directoryToList) {
            console.Error("Usage: .ls <directory>")
            return CommandResult(context, null, false)
        }

        const files = List(directoryToList)
        if (files.length === 0) {
            console.Info(" ")
            return CommandResult(context, [])
        }

        for (const { name, type, size, layer } of files) {
            console.Info(`${name} (${type} at ${layer}) - ${size} bytes`)
        }

        return CommandResult(context, files)
    }
}
