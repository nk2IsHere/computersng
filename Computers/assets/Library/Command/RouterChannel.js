import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { ConfigureRouter, GetRouters } from "../Core/Network"

export default {
    command: "router-channel",
    description: "Set or clear a covering router's channel",
    usage: ".router-channel <routerAddress> <channel|clear>",
    action: async (args, console, context) => {
        const [routerAddress, channelInput] = CommandArguments(context, args)
        if (routerAddress === undefined || channelInput === undefined) {
            console.Error("Usage: .router-channel <routerAddress> <channel|clear>")
            return CommandResult(context, null, false)
        }

        const channel = channelInput === "clear" ? null : Number.parseInt(channelInput, 10)
        if (channel !== null && Number.isNaN(channel)) {
            console.Error("Channel must be an integer or 'clear'")
            return CommandResult(context, null, false)
        }

        try {
            ConfigureRouter(routerAddress, channel)
            console.Info(`Router ${routerAddress} channel set to ${channel ?? "none"}`)
            console.Info(`Covering routers now: ${JSON.stringify(GetRouters())}`)
            return CommandResult(context)
        } catch (e) {
            console.Error(`Configure failed: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
