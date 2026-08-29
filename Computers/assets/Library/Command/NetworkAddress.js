import { CommandResult } from "../Core/Utils/Command"
import { GetAddress } from "../Core/Network"

export default {
    command: "net-addr",
    description: "Print this computer's network address",
    usage: ".net-addr",
    action: async (args, console, context) => {
        console.Info(`Network address: ${GetAddress()}`)
        return CommandResult(context, GetAddress())
    }
}
