import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { SendMessage } from "../Core/Network"

export default {
    command: "net-send",
    description: "Send a text message to a computer by address",
    usage: ".net-send <address> <text>",
    action: async (args, console, context) => {
        const [address, ...textParts] = CommandArguments(context, args)
        if (address === undefined || textParts.length === 0) {
            console.Error("Usage: .net-send <address> <text>")
            return CommandResult(context, null, false)
        }

        try {
            SendMessage(address, textParts.join(" "))
            console.Info(`Sent to ${address}`)
            return CommandResult(context)
        } catch (e) {
            console.Error(`Send failed: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
