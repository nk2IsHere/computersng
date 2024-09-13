
export class Cache {
    constructor({ valueProvider, expireIn }) {
        this.valueProvider = valueProvider
        this.expireIn = expireIn
        this.cachedValue = valueProvider()
        this.expirationTime = System.Time() + expireIn
    }

    ProvideValue() {
        if (System.Time() > this.expirationTime) {
            this.cachedValue = this.valueProvider()
            this.expirationTime = System.Time() + this.expireIn
        }
        
        return this.cachedValue
    }
}
