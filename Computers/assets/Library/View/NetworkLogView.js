import { FireResult } from "./FireResult"

export class NetworkLogView {
    constructor(console) {
        this.console = console
    }

    Fire(event) {
        if (event.Type === "NetworkMessage") {
            const [sourceAddress, payload] = event.Data
            this.console.Info(`[${sourceAddress}] ${payload}`)
        }
        return FireResult.Passed
    }

    Render() {
    }
}
