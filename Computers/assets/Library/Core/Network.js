import { AddListener } from "./Events"
import { Call } from "./Rpc"

export async function HttpRequestBytes(url, method = 'GET', data = null, headers = {}) {
    if(typeof url !== 'string') {
        throw new Error('url must be a string')
    }
    
    if(typeof method !== 'string') {
        throw new Error('method must be a string')
    }
    
    if(data !== null && !Array.isArray(data)) {
        throw new Error('data must be a byte array or null')
    }
    
    if(headers !== null && typeof headers !== 'object') {
        throw new Error('headers must be an object or null')
    }
    
    if(!['GET', 'POST', 'PUT', 'DELETE'].includes(method)) {
        throw new Error('method must be a valid HTTP method')
    }
    
    headers = new Map(Object.entries(headers ?? {}));

    const { StatusCode, Headers, Body } = await Network.RequestHttpBytes(url, method, headers, data);
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpRequestString(url, method = 'GET', data = null, headers = {}) {
    if(typeof url !== 'string') {
        throw new Error('url must be a string')
    }
    
    if(typeof method !== 'string') {
        throw new Error('method must be a string')
    }
    
    if(data !== null && typeof data !== 'string') {
        throw new Error('data must be a string or null')
    }
    
    if(headers !== null && typeof headers !== 'object') {
        throw new Error('headers must be an object or null')
    }
    
    if(!['GET', 'POST', 'PUT', 'DELETE'].includes(method)) {
        throw new Error('method must be a valid HTTP method')
    }
    
    headers = new Map(Object.entries(headers ?? {}));

    const { StatusCode, Headers, Body } = await Network.RequestHttpString(url, method, headers, data);
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export function SendMessage(address, payload) {
    if(typeof address !== 'string') {
        throw new Error('address must be a string')
    }

    if(typeof payload !== 'string') {
        throw new Error('payload must be a string')
    }

    Network.SendMessage(address, payload)
}

export function SendJson(address, data) {
    SendMessage(address, JSON.stringify(data))
}

export function GetAddress() {
    return Network.GetAddress()
}

export function GetRouters() {
    return [...Network.GetRouters()].map(router => ({
        address: router.Address,
        channel: router.Channel ?? null
    }))
}

let nextDiscoverCid = 1

export async function Discover({ collectFrames = 120 } = {}) {
    const cid = `d${nextDiscoverCid++}`
    const found = new Set()

    const unsubscribe = AddListener((event) => {
        if (event.Type !== "NetworkMessage") {
            return false
        }
        const [, payload] = event.Data
        try {
            const parsed = JSON.parse(payload)
            if (parsed !== null && parsed.re === cid && parsed.ok === true) {
                for (const address of parsed.data.endpoints) {
                    found.add(address)
                }
                return true
            }
        } catch { /* not for us */ }
        return false
    })

    try {
        SendJson("*", { cid, cmd: "discover" })
        for (let frame = 0; frame < collectFrames; frame++) {
            await System.NextFrame()
        }
    } finally {
        unsubscribe()
    }

    found.delete(GetAddress())
    return [...found].sort()
}

export async function ConfigureRouter(routerAddress, channel, options = {}) {
    if (typeof routerAddress !== 'string') {
        throw new Error('routerAddress must be a string')
    }

    if (channel !== null && !Number.isInteger(channel)) {
        throw new Error('channel must be an integer or null')
    }

    await Call(routerAddress, "configure", { channel }, options)
}
