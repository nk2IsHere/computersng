import { Call } from "./Rpc"

export { Call } from "./Rpc"

export async function Subscribe(address, events, options = {}) {
    await Call(address, "subscribe", { events }, options)

    return {
        Drain(pumpedEvents) {
            const pushes = []
            for (const event of pumpedEvents) {
                if (event.Type !== "NetworkMessage") continue
                const [sourceAddress, payload] = event.Data
                if (sourceAddress !== address) continue
                try {
                    const parsed = JSON.parse(payload)
                    if (parsed.event !== undefined) pushes.push(parsed)
                } catch { /* not an event push */ }
            }
            return pushes
        },
        async Unsubscribe() {
            await Call(address, "unsubscribe", { events }, options)
        }
    }
}
