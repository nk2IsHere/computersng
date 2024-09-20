import { CommandResult } from "../Core/Utils/Command"

export default {
    command: "clear",
    description: "Clear console",
    usage: ".clear",
    action: async (args, console, context) => {
        console.Clear()
        return CommandResult(context)
    }
}
