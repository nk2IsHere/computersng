import { Keys } from "./Constants"

export class ReloadView {
    constructor(onReload) {
        this.onReload = onReload
    }

    Fire(event) {
        if(event.Type === "KeyPressed") {
            const [keyRaw] = event.Data
            const key = Keys[keyRaw]
            if (key.name === "F1") {
                this.onReload()
            }
        }
    }
    
    Render() {
    }
}
