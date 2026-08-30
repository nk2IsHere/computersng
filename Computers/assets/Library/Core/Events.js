const listeners = new Set()

export function AddListener(listener) {
    listeners.add(listener)
    return () => listeners.delete(listener)
}

export function DispatchToListeners(event) {
    let claimed = false
    for (const listener of [...listeners]) {
        try {
            if (listener(event) === true) {
                claimed = true
            }
        } catch { /* a listener error must not break dispatch */ }
    }
    return claimed
}
