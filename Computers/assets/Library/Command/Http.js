import { HttpRequestString } from "../Core/Network"
import { CommandArguments, CommandResult } from "../Core/Utils"

export default {
    command: "http",
    description: "HTTP request",
    usage: ".http <method> <url> [--headers key1=value1 key2=value2] [--data <data>]",
    action: async (args, console, context) => {
        const parsedArguments = CommandArguments(context, args)
        const method = parsedArguments[0]
        const url = parsedArguments[1]

        if (!method || !url) {
            console.Error("Usage: .http <method> <url> [--headers key1=value1 key2=value2] [--data <data>]")
            return CommandResult(context, null, false)
        }

        const headers = {}
        let data = null

        for (let i = 2; i < parsedArguments.length; i++) {
            const arg = parsedArguments[i]

            if (arg === "--headers") {
                const headerArgs = parsedArguments[i + 1]
                for (const headerArg of headerArgs) {
                    const [key, value] = headerArg.split("=")
                    headers[key] = value
                }

                i++
                continue
            }

            if (arg === "--data") {
                data = parsedArguments[i + 1]
                i++
                continue
            }

            console.Error(`Unknown argument: ${arg}`)
        }

        const response = await HttpRequestString(url, method, data, headers)
        if (response.statusCode >= 400) {
            console.Error(`Request failed with status code ${response.statusCode}`)
            return CommandResult(context, response.body, false)
        }

        console.Info(`Request successful with status code ${response.statusCode}`)
        return CommandResult(context, response.body)
    }
}
