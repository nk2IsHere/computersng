import { SendJson } from "./Network"
import { AddListener } from "./Events"

let nextCid = 1

export async function Call(address, cmd, args = {}, options = {}) {
    const { timeoutFrames = 120 } = options
    const cid = `c${nextCid++}`

    let reply = null
    const unsubscribe = AddListener((event) => {
        if (event.Type !== "NetworkMessage") {
            return false
        }
        const [, payload] = event.Data
        try {
            const parsed = JSON.parse(payload)
            if (parsed !== null && parsed.re === cid) {
                reply = parsed
                // Claiming keeps the reply out of the OS loop log
                return true
            }
        } catch { /* not for us */ }
        return false
    })

    try {
        SendJson(address, { cid, cmd, ...args })

        for (let frame = 0; frame <= timeoutFrames; frame++) {
            if (reply !== null) {
                if (!reply.ok) {
                    throw new Error(reply.error ?? "peripheral error")
                }
                return reply.data
            }
            await System.NextFrame()
        }

        throw new Error(`Call to ${address} timed out (${cmd})`)
    } finally {
        unsubscribe()
    }
}
