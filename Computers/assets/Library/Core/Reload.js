
export class ReloadView {
    constructor(onReload) {
        this.onReload = onReload
    }

    Fire(event) {
        if(event.Type === "KeyPressed") {
            const [key] = event.Data
            if (key === 112) { // F1
                this.onReload()
            }
        }
    }
    
    Render() {
    }
}
