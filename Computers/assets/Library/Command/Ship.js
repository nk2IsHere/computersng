import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { Call } from "../Core/Rpc"

export default {
    command: "ship",
    description: "Sell items from a shipping controller's adjacent chests",
    usage: ".ship <address> <itemId> [count]",
    action: async (args, console, context) => {
        const [address, itemId, countInput] = CommandArguments(context, args)
        if (address === undefined || itemId === undefined) {
            console.Error("Usage: .ship <address> <itemId> [count]")
            return CommandResult(context, null, false)
        }

        const callArgs = { itemId }
        if (countInput !== undefined) {
            const count = Number.parseInt(countInput, 10)
            if (Number.isNaN(count)) {
                console.Error("Count must be an integer")
                return CommandResult(context, null, false)
            }
            callArgs.count = count
        }

        try {
            const result = await Call(address, "sell", callArgs)
            console.Info(`Shipped ${result.sold}x ${result.name} for ${result.value}g`)
            return CommandResult(context, result)
        } catch (e) {
            console.Error(`Ship failed: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
