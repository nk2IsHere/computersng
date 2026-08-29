import { CommandResult } from "../Core/Utils/Command"
import { ListReachable, GetRouters } from "../Core/Network"

export default {
    command: "net-ls",
    description: "List reachable computers and covering routers",
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

        const reachable = ListReachable()
        if (reachable.length === 0) {
            console.Info("No reachable computers")
        }
        for (const address of reachable) {
            console.Info(`-> ${address}`)
        }

        return CommandResult(context, reachable)
    }
}
