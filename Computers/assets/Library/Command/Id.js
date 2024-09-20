import { CommandResult } from "../Core/Utils/Command"

export default {
    command: "id",
    description: "Print system identification",
    usage: ".id",
    action: async (args, console, context) => {
        console.Info(`System identification: ${System.Id()}`)
        return CommandResult(context)
    }
}
