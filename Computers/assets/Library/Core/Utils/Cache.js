
export class Cache {
    constructor({ valueProvider, expireIn }) {
        this.valueProvider = valueProvider
        this.expireIn = expireIn
    }

    ProvideValue(context) {
        if (this.cachedValue === undefined) {
            this.cachedValue = this.valueProvider(context)
            this.expirationTime = System.Time() + this.expireIn
        }
        
        if (System.Time() > this.expirationTime) {
            this.cachedValue = this.valueProvider(context)
            this.expirationTime = System.Time() + this.expireIn
        }
        
        return this.cachedValue
    }
}
