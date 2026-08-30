import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { Call } from "../Core/Rpc"

export default {
    command: "machines",
    description: "List a machine controller's group",
    usage: ".machines <address>",
    action: async (args, console, context) => {
        const [address] = CommandArguments(context, args)
        if (address === undefined) {
            console.Error("Usage: .machines <address>")
            return CommandResult(context, null, false)
        }

        try {
            const snapshot = await Call(address, "list", {})

            if (snapshot.truncated) {
                console.Warning("Group truncated by peripheral.maxGroupSize")
            }

            if (snapshot.machines.length === 0) {
                console.Info("No machines in group")
            }
            for (const machine of snapshot.machines) {
                const details = []
                if (machine.heldItem) details.push(machine.heldItem)
                if (machine.state === "working") details.push(`${machine.minutesUntilReady}m`)
                const suffix = details.length > 0 ? ` (${details.join(", ")})` : ""
                console.Info(`[${machine.x},${machine.y}] ${machine.name}: ${machine.state}${suffix}`)
            }

            for (const chest of snapshot.chests) {
                const summary = chest.items.map(item => `${item.name} x${item.count}`).join(", ")
                console.Info(`[${chest.x},${chest.y}] Chest: ${summary.length > 0 ? summary : "empty"}`)
            }

            return CommandResult(context, snapshot)
        } catch (e) {
            console.Error(`Failed to list machines: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
