import { CommandResult } from "../Core/Utils"

export default {
    command: "pwd",
    description: "Print working directory",
    usage: ".pwd",
    action: async (args, console, context) => {
        console.Info(context.currentDirectory)
        return CommandResult(context, context.currentDirectory)
    }
}
