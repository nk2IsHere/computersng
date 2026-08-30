import { CommandResult, CommandArguments } from "../Core/Utils/Command"
import { Call } from "../Core/Rpc"

export default {
    command: "peripheral",
    description: "Send an RPC command to a peripheral",
    usage: ".peripheral <address> <cmd> [json-args]",
    action: async (args, console, context) => {
        const [address, cmd, rawArgs] = CommandArguments(context, args)
        if (address === undefined || cmd === undefined) {
            console.Error("Usage: .peripheral <address> <cmd> [json-args]")
            return CommandResult(context, null, false)
        }

        let callArgs = {}
        if (rawArgs !== undefined) {
            try {
                callArgs = JSON.parse(rawArgs)
            } catch (e) {
                console.Error(`Invalid json-args: ${e.message}`)
                return CommandResult(context, null, false)
            }
        }

        try {
            const result = await Call(address, cmd, callArgs)
            console.Info(JSON.stringify(result))
            return CommandResult(context, result)
        } catch (e) {
            console.Error(`Peripheral call failed: ${e.message}`)
            return CommandResult(context, null, false)
        }
    }
}
