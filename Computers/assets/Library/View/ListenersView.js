import { DispatchToListeners } from "../Core/Events"
import { FireResult } from "./FireResult"

export class ListenersView {
    Fire(event) {
        return DispatchToListeners(event) ? FireResult.Claimed : FireResult.Passed
    }

    Render() {
    }
}
