import { JoinPaths, ReadString } from "../Core/Storage"
import { CommandArguments, CommandResult } from "../Core/Utils"

export default {
    command: "cat",
    description: "Print file content",
    usage: ".cat <file>",
    action: async (args, console, context) => {
        const [file] = CommandArguments(context, args)

        if (!file) {
            console.Error("Usage: .cat <file>")
            return
        }

        const content = ReadString(JoinPaths(context.currentDirectory, file))

        console.Info(content)
        return CommandResult(context, content)
    }
}
