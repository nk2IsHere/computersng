import { JoinPaths, WriteString } from "../Core/Storage"
import { CommandArguments, CommandResult } from "../Core/Utils/Command"

export default {
    command: "write",
    description: "Write content to file",
    usage: ".write <file> <content>",
    action: async (args, console, context) => {
        const [file, content] = CommandArguments(context, args)

        if (!file || !content) {
            console.Error("Usage: .write <file> <content>")
            return CommandResult(context, null, false)
        }

        const path = JoinPaths(context.currentDirectory, file)
        WriteString(path, content)

        console.Info(`Wrote content to ${path}`)
        return CommandResult(context)
    }
}
