import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { Call } from "../Core/Rpc"

export default {
    command: "weather",
    description: "Read a weather station",
    usage: ".weather <address>",
    action: async (args, console, context) => {
        const [address] = CommandArguments(context, args)
        if (address === undefined) {
            console.Error("Usage: .weather <address>")
            return CommandResult(context, null, false)
        }

        try {
            const reading = await Call(address, "read", {})
            const hours = Math.floor(reading.time / 100)
            const minutes = `${reading.time % 100}`.padStart(2, "0")
            console.Info(`Day ${reading.day} of ${reading.season}, year ${reading.year}, ${hours}:${minutes}`)
            console.Info(`Weather: ${reading.weather} (tomorrow: ${reading.weatherTomorrow})`)
            console.Info(`Daily luck: ${reading.dailyLuck}`)
            return CommandResult(context, reading)
        } catch (e) {
            console.Error(`Weather read failed: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
