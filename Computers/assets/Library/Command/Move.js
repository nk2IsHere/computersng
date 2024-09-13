import { Copy } from "../Core/Storage"
import { CommandArguments, CommandResult } from "../Core/Utils"

export default {
    command: "mv",
    description: "Move file or directory",
    usage: ".mv [--recursive] [--overwrite] <source> <destination>",
    action: async (args, console, context) => {
        const parsedArguments = CommandArguments(context, args)

        const recursive = parsedArguments.includes("--recursive")
        const overwrite = parsedArguments.includes("--overwrite")

        const source = parsedArguments[parsedArguments.length - 2]
        const destination = parsedArguments[parsedArguments.length - 1]

        if (!source || !destination) {
            console.Error("Usage: .mv [--overwrite] <source> <destination>")
            return CommandResult(context, null, false)
        }

        Copy(source, destination, { move: true, overwrite, recursive })
        console.Info(`Moved ${source} to ${destination}`)

        return CommandResult(context)
    }
}
