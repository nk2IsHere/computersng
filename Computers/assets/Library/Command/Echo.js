import { CommandArguments, CommandResult } from "../Core/Utils/Command"

export default {
    command: "echo",
    description: "Print message",
    usage: ".echo <message>",
    action: async (args, console, context) => {
        const [message] = CommandArguments(context, args)

        if (!message) {
            console.Error("Usage: .echo <message>")
            return CommandResult(context, null, false)
        }

        console.Info(message)
        return CommandResult(context, message)
    }
}
