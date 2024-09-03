
export function List(path) {
    const response = Storage.List(path);
    if (response.Type === "Error") {
        throw new Error(response.Error)
    }
    
    return response.Data
        .map(({ Name, Type, Size }) => ({
            name: Name,
            type: Type,
            size: Size
        }))
}

export function ReadBytes(path) {
    const response = Storage.Read(path);
    if (response.Type === "Error") {
        throw new Error(response.Error)
    }
    
    return response.Data
}

export function ReadString(path) {
    return Array
        .from(ReadBytes(path))
        .map(byte => String.fromCharCode(byte))
        .join('')
}

export function WriteBytes(path, data) {
    const response = Storage.Write(path, data);
    if (response.Type === "Error") {
        throw new Error(response.Error)
    }
    
    return response.Data
}

export function WriteString(path, data) {
    return WriteBytes(path, data.split('').map(char => char.charCodeAt(0)))
}

export function Delete(path, recursive = false) {
    const response = Storage.Delete(path, recursive);
    if (response.Type === "Error") {
        throw new Error(response.Error)
    }
    
    return response.Data
}

export function Exists(path) {
    return Storage.Exists(path)
}

export function MakeDirectory(path) {
    const response = Storage.MakeDirectory(path);
    if (response.Type === "Error") {
        throw new Error(response.Error)
    }
    
    return response.Data
}
