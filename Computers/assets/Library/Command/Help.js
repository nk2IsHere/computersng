import { CommandResult } from "../Core/Utils/Command"

export default {
    command: "help",
    description: "Prints this help message",
    usage: ".help",
    action: async (args, console, context) => {
        console.Info("Available commands:")
        for (const { command, description, usage } of context.consoleCommands) {
            console.Info(`-> .${command}: ${description} (${usage})`)
        }

        return CommandResult(context, null)
    }
}
