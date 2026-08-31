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

            const machines = snapshot.members.filter(member => member.kind === "machine")
            const chests = snapshot.members.filter(member => member.kind === "chest")
            const connectors = snapshot.members.filter(member => member.kind === "connector")
            const ready = machines.filter(machine => machine.state === "ready")

            const counts = [`${machines.length} machines (${ready.length} ready)`, `${chests.length} chests`]
            if (connectors.length > 0) counts.push(`${connectors.length} connectors`)
            console.Info(counts.join(", "))

            if (snapshot.members.length === 0) {
                console.Info("No members in group")
            }

            const stateOrder = { ready: 0, working: 1, empty: 2 }
            machines.sort((a, b) => stateOrder[a.state] - stateOrder[b.state])
            for (const machine of machines) {
                const details = []
                if (machine.heldItem) details.push(machine.heldItem)
                if (machine.state === "working") details.push(`${machine.minutesUntilReady}m left`)
                const suffix = details.length > 0 ? ` (${details.join(", ")})` : ""
                console.Info(`[${machine.x},${machine.y}] ${machine.name}: ${machine.state}${suffix}`)
            }
            for (const chest of chests) {
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
