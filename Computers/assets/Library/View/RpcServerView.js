import { SendJson } from "../Core/Network"
import { FireResult } from "./FireResult"

function TryReply(address, reply) {
    try {
        SendJson(address, reply)
    } catch { }
}

export class RpcServerView {
    constructor(handlers = {}) {
        this.handlers = new Map(Object.entries(handlers))
    }

    Fire(event) {
        if (event.Type !== "NetworkMessage" || this.handlers.size === 0) {
            return FireResult.Passed
        }
        const [sourceAddress, payload] = event.Data

        let parsed
        try {
            parsed = JSON.parse(payload)
        } catch {
            return FireResult.Passed
        }
        if (parsed === null || typeof parsed !== "object") {
            return FireResult.Passed
        }

        // A message is a request when it has cid and cmd and no re
        if (parsed.cid === undefined || parsed.cmd === undefined || parsed.re !== undefined) {
            return FireResult.Passed
        }

        const { cid, cmd, ...args } = parsed
        const handler = this.handlers.get(cmd)
        if (handler === undefined) {
            TryReply(sourceAddress, { re: cid, ok: false, error: "unknown command" })
            return FireResult.Claimed
        }

        Promise.resolve()
            .then(() => handler(args, sourceAddress))
            .then(data => TryReply(sourceAddress, { re: cid, ok: true, data: data ?? null }))
            .catch(e => TryReply(sourceAddress, { re: cid, ok: false, error: String(e && e.message || e) }))
        return FireResult.Claimed
    }

    Render() {
    }
}
