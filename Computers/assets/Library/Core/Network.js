
export async function HttpGetBytes(url, headers = {}) {
    const { StatusCode, Headers, Body } = await Network.RequestHttpBytes(url, 'GET', headers)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpGetText(url, headers = {}) {
    const { StatusCode, Headers, Body } = await Network.RequestHttpString(url, 'GET', headers)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpPostBytes(url, data, headers = {}) {
    if(!Array.isArray(data)) {
        throw new Error('data must be an array of bytes')
    }
    
    const { StatusCode, Headers, Body } = await Network.RequestHttpBytes(url, 'POST', headers, data)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpPostText(url, data, headers = {}) {
    if(typeof data !== 'string') {
        throw new Error('data must be a string')
    }
    
    const { StatusCode, Headers, Body } = await Network.RequestHttpString(url, 'POST', headers, data)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpPutBytes(url, data, headers = {}) {
    if(!Array.isArray(data)) {
        throw new Error('data must be an array of bytes')
    }
    
    const { StatusCode, Headers, Body } = await Network.RequestHttpBytes(url, 'PUT', headers, data)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpPutText(url, data, headers = {}) {
    if(typeof data !== 'string') {
        throw new Error('data must be a string')
    }
    
    const { StatusCode, Headers, Body } = await Network.RequestHttpString(url, 'PUT', headers, data)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpDeleteBytes(url, headers = {}) {
    const { StatusCode, Headers, Body } = await Network.RequestHttpBytes(url, 'DELETE', headers)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpDeleteText(url, headers = {}) {
    const { StatusCode, Headers, Body } = await Network.RequestHttpString(url, 'DELETE', headers)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpHead(url, headers = {}) {
    const { StatusCode, Headers, Body } = await Network.RequestHttpString(url, 'HEAD', headers)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpOptions(url, headers = {}) {
    const { StatusCode, Headers, Body } = await Network.RequestHttpString(url, 'OPTIONS', headers)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpPatchBytes(url, data, headers = {}) {
    if(!Array.isArray(data)) {
        throw new Error('data must be an array of bytes')
    }
    
    const { StatusCode, Headers, Body } = await Network.RequestHttpBytes(url, 'PATCH', headers, data)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}

export async function HttpPatchText(url, data, headers = {}) {
    if(typeof data !== 'string') {
        throw new Error('data must be a string')
    }
    
    const { StatusCode, Headers, Body } = await Network.RequestHttpString(url, 'PATCH', headers, data)
    return {
        statusCode: StatusCode,
        headers: Headers,
        body: Body
    }
}
