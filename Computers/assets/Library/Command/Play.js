import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { Call } from "../Core/Rpc"

export default {
    command: "play",
    description: "Play a sound cue through a speaker",
    usage: ".play <address> <cue> [pitch]",
    action: async (args, console, context) => {
        const [address, cue, pitchInput] = CommandArguments(context, args)
        if (address === undefined || cue === undefined) {
            console.Error("Usage: .play <address> <cue> [pitch]")
            return CommandResult(context, null, false)
        }

        const callArgs = { cue }
        if (pitchInput !== undefined) {
            const pitch = Number.parseInt(pitchInput, 10)
            if (Number.isNaN(pitch)) {
                console.Error("Pitch must be an integer")
                return CommandResult(context, null, false)
            }
            callArgs.pitch = pitch
        }

        try {
            await Call(address, "play", callArgs)
            return CommandResult(context)
        } catch (e) {
            console.Error(`Play failed: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
