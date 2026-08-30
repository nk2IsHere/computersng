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
                console.Warning("Group truncated by machineController.maxGroupSize")
            }

            if (snapshot.members.length === 0) {
                console.Info("No members in group")
            }
            for (const member of snapshot.members) {
                if (member.kind === "machine") {
                    const details = []
                    if (member.heldItem) details.push(member.heldItem)
                    if (member.state === "working") details.push(`${member.minutesUntilReady}m`)
                    const suffix = details.length > 0 ? ` (${details.join(", ")})` : ""
                    console.Info(`[${member.x},${member.y}] ${member.name}: ${member.state}${suffix}`)
                } else if (member.kind === "chest") {
                    const summary = member.items.map(item => `${item.name} x${item.count}`).join(", ")
                    console.Info(`[${member.x},${member.y}] Chest: ${summary.length > 0 ? summary : "empty"}`)
                } else {
                    console.Info(`[${member.x},${member.y}] ${member.kind}`)
                }
            }

            return CommandResult(context, snapshot)
        } catch (e) {
            console.Error(`Failed to list machines: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
