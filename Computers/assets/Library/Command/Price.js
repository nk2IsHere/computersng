import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { Call } from "../Core/Rpc"

export default {
    command: "price",
    description: "Query an item's sell price through a shipping controller",
    usage: ".price <address> <itemId>",
    action: async (args, console, context) => {
        const [address, itemId] = CommandArguments(context, args)
        if (address === undefined || itemId === undefined) {
            console.Error("Usage: .price <address> <itemId>")
            return CommandResult(context, null, false)
        }

        try {
            const result = await Call(address, "price", { itemId })
            console.Info(`${result.itemId} sells for ${result.price}g`)
            return CommandResult(context, result)
        } catch (e) {
            console.Error(`Price failed: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
