
const StorageErrorType = Object.freeze({
    0: "FileNotFound",
    1: "DirectoryNotFound",
    2: "FileAlreadyExists",
    3: "DirectoryAlreadyExists",
    4: "DirectoryNotEmpty",
    5: "PathIsNotDirectory",
    6: "PathIsNotFile"
})

const StorageResponseType = Object.freeze({
    0: "Success",
    1: "Error"
})

const StorageFileType = Object.freeze({
    0: "File",
    1: "Directory"
})

export function List(path) {
    const response = Storage.List(path);
    if (StorageResponseType[response.Type] === "Error") {
        throw new Error(StorageErrorType[response.Error])
    }
    
    return response.Data
        .map(({ Name, Type, Size }) => ({
            name: Name,
            type: StorageFileType[Type],
            size: Size
        }))
}

export function ReadBytes(path) {
    const response = Storage.Read(path);
    if (StorageResponseType[response.Type] === "Error") {
        throw new Error(StorageErrorType[response.Error])
    }
    
    const file = response.Data
    return file.Data
}

export function ReadString(path) {
    return Array
        .from(ReadBytes(path))
        .map(byte => String.fromCharCode(byte))
        .join('')
}

export function ReadMetadata(path) {
    const response = Storage.ReadMetadata(path);
    if (StorageResponseType[response.Type] === "Error") {
        throw new Error(StorageErrorType[response.Error])
    }
    
    return response.Data
}

export function WriteBytes(path, data) {
    const response = Storage.Write(path, data);
    if (StorageResponseType[response.Type] === "Error") {
        throw new Error(StorageErrorType[response.Error])
    }
    
    return response.Data
}

export function WriteString(path, data) {
    return WriteBytes(path, data.split('').map(char => char.charCodeAt(0)))
}

export function Delete(path, { recursive = false } = {}) {
    const response = Storage.Delete(path, recursive);
    if (StorageResponseType[response.Type] === "Error") {
        throw new Error(StorageErrorType[response.Error])
    }
    
    return response.Data
}

export function Exists(path) {
    return Storage.Exists(path)
}

export function MakeDirectory(path) {
    const response = Storage.MakeDirectory(path);
    if (StorageResponseType[response.Type] === "Error") {
        throw new Error(StorageErrorType[response.Error])
    }
    
    return response.Data
}

function joinPathWithPart(path, pathPart) {
    // Path parts can include .. and . to navigate up and down the directory tree
    const cleanedPath = path.lastIndexOf('/') === path.length - 1
        ? path.slice(0, -1)
        : path;
    
    if (pathPart === '..') {
        const cleanedPathParts = cleanedPath.split('/');
        cleanedPathParts.pop();
        return `${cleanedPathParts.join('/')}/`
    }
    
    if (pathPart === '.') {
        return `${cleanedPath}/`
    }
    
    return `${cleanedPath}/${pathPart}`
}

export function JoinPaths(path, pathParts) {
    const cleanedPathPaths = pathParts
        .split('/')
        .filter(part => part !== '');
    
    return cleanedPathPaths.reduce(joinPathWithPart, path)
}

export function Copy(source, destination, { move = false, overwrite = false, recursive = false } = {}) {
    if (!source || !destination) {
        throw new Error('Source and destination paths must be provided')
    }
    
    if(!Exists(source)) {
        throw new Error(`Source path does not exist: ${source}`)
    }
    
    if (Exists(destination) && !overwrite) {
        throw new Error(`Destination path already exists: ${destination}`)
    }
    
    const sourceMetadata = ReadMetadata(source);
    if (StorageFileType[sourceMetadata.Type] === "File" && !recursive) {
        throw new Error(`Source path is a directory and recursive copy is not enabled: ${source}`)
    }
    
    if (StorageFileType[sourceMetadata.Type] !== "Directory") {
        if (Exists(destination)) {
            Delete(destination, { recursive: true })
        }
        
        WriteBytes(destination, ReadBytes(source))
        if (move) {
            Delete(source)
        }
    }

    if (Exists(destination)) {
        Delete(destination, { recursive: true })
    }
    
    MakeDirectory(destination);
    List(source)
        .forEach(({ name }) => {
            Copy(
                JoinPaths(source, name),
                JoinPaths(destination, name),
                { move, overwrite, recursive }
            )
        })
    
    if (move) {
        Delete(source, { recursive: true })
    }
}
