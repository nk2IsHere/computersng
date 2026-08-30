import { CommandResult } from "../Core/Utils/Command"
import { Discover, GetRouters } from "../Core/Network"
import { Call } from "../Core/Rpc"

export default {
    command: "net-ls",
    description: "List covering routers, discover reachable endpoints and ask who they are",
    usage: ".net-ls",
    action: async (args, console, context) => {
        const routers = GetRouters()
        if (routers.length === 0) {
            console.Warning("Offline: no router in range")
            return CommandResult(context, null, false)
        }

        for (const { address, channel } of routers) {
            console.Info(`Router ${address} (channel: ${channel ?? "none"})`)
        }

        console.Info("Discovering...")
        const reachable = await Discover()
        if (reachable.length === 0) {
            console.Info("No endpoints discovered")
            return CommandResult(context, [])
        }

        const identified = await Promise.all(reachable.map(async address => {
            try {
                const info = await Call(address, "ping", {}, { timeoutFrames: 60 })
                return { address, type: info?.type ?? "unknown" }
            } catch {
                return { address, type: "silent" }
            }
        }))

        for (const { address, type } of identified) {
            console.Info(`-> ${address} (${type})`)
        }

        return CommandResult(context, identified)
    }
}
