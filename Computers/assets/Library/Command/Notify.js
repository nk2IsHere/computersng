import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { Call } from "../Core/Rpc"

export default {
    command: "notify",
    description: "Show a HUD notification through a mailer",
    usage: ".notify <address> <text...>",
    action: async (args, console, context) => {
        const [address, ...words] = CommandArguments(context, args)
        if (address === undefined || words.length === 0) {
            console.Error("Usage: .notify <address> <text...>")
            return CommandResult(context, null, false)
        }

        try {
            await Call(address, "notify", { text: words.join(" ") })
            console.Info("Notified")
            return CommandResult(context)
        } catch (e) {
            console.Error(`Notify failed: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
