import { CommandResult } from "../Core/Utils"

export default {
    command: "clear",
    description: "Clear console",
    usage: ".clear",
    action: async (args, console, context) => {
        console.Clear()
        return CommandResult(context)
    }
}
