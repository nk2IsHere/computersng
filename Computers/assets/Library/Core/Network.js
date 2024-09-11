
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
    
    const { Result } = await Network.RequestHttpBytes(url, method, headers, data);
    const { StatusCode, Headers, Body } = Result;
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
    
    const { Result } = await Network.RequestHttpString(url, method, headers, data);
    const { StatusCode, Headers, Body } = Result;
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}
