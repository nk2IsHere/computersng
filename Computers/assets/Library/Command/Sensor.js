import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { Call } from "../Core/Rpc"

export default {
    command: "sensor",
    description: "Read a player sensor, optionally setting its radius first",
    usage: ".sensor <address> [radius]",
    action: async (args, console, context) => {
        const [address, radiusInput] = CommandArguments(context, args)
        if (address === undefined) {
            console.Error("Usage: .sensor <address> [radius]")
            return CommandResult(context, null, false)
        }

        try {
            if (radiusInput !== undefined) {
                const radius = Number.parseInt(radiusInput, 10)
                if (Number.isNaN(radius)) {
                    console.Error("Radius must be an integer")
                    return CommandResult(context, null, false)
                }
                await Call(address, "configure", { radius })
                console.Info(`Radius set to ${radius}`)
            }

            const info = await Call(address, "ping", {})
            const reading = await Call(address, "read", {})
            console.Info(`Sensor radius: ${info.radius}`)
            if (reading.players.length === 0 && reading.npcs.length === 0) {
                console.Info("Nobody nearby")
            }
            for (const character of [...reading.players, ...reading.npcs]) {
                console.Info(`[${character.x},${character.y}] ${character.name} (${character.kind})`)
            }
            return CommandResult(context, reading)
        } catch (e) {
            console.Error(`Sensor failed: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
