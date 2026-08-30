import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { Call } from "../Core/Rpc"

export default {
    command: "mail",
    description: "Queue an in-game letter for tomorrow through a mailer",
    usage: ".mail <address> <text...>",
    action: async (args, console, context) => {
        const [address, ...words] = CommandArguments(context, args)
        if (address === undefined || words.length === 0) {
            console.Error("Usage: .mail <address> <text...>")
            return CommandResult(context, null, false)
        }

        try {
            const result = await Call(address, "mail", { text: words.join(" ") })
            console.Info(`Letter ${result.mailId} arrives tomorrow`)
            return CommandResult(context, result)
        } catch (e) {
            console.Error(`Mail failed: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
