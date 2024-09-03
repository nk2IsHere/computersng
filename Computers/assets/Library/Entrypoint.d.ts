
declare const Render: {
    Begin: () => void
    End: () => void
    GetScreenBoundaries: () => [number, number]
    Rectangle: (x: number, y: number, width: number, height: number, color: [number, number, number, number]) => void
    Text: (x: number, y: number, text: string, size: number, color: [number, number, number, number]) => void
    BorderRectangle: (x: number, y: number, width: number, height: number, borderWidth: number, color: [number, number, number, number]) => void
    Circle: (x: number, y: number, radius: number, color: [number, number, number, number]) => void
    BorderCircle: (x: number, y: number, radius: number, borderWidth: number, color: [number, number, number, number]) => void
    Line: (x1: number, y1: number, x2: number, y2: number, color: [number, number, number, number]) => void
    ClearBackground: (color: [number, number, number, number]) => void
    ClearForeground: () => void
    SetBackground: (x: number, y: number, color: [number, number, number, number]) => void
    SetForeground: (x: number, y: number, color: [number, number, number, number]) => void
    GetMaximalFontSize: () => number
    GetDefaultFontSize: () => number
    MeasureTextWidth: (text: string, size: number) => [number, number]
    MeasureGlyphSize: (char: string, size: number) => [number, number]
}

declare type EventData = {
    Type: "Tick" 
        | "KeyPressed"
        | "MouseLeftClicked"
        | "MouseRightClicked"
        | "MouseWheel"
        | "ButtonHeld"
        | "ButtonUnheld"
    Data: any[]
}

// @ts-ignore
declare const Event: {
    Poll: () => EventData[]
}

declare const System: {
    Sleep: (ms: number) => void
    Time: () => number
}

declare enum StorageErrorType {
    FileNotFound = "FileNotFound",
    DirectoryNotFound = "DirectoryNotFound",
    FileAlreadyExists = "FileAlreadyExists",
    DirectoryAlreadyExists = "DirectoryAlreadyExists",
    DirectoryNotEmpty = "DirectoryNotEmpty",
    PathIsNotDirectory = "PathIsNotDirectory",
    PathIsNotFile = "PathIsNotFile",
}

declare enum StorageResponseType {
    Success = "Success",
    Error = "Error",
}

declare type StorageResponse<T> = 
    | { Type: StorageResponseType.Success, Data: T }
    | { Type: StorageResponseType.Error, Error: StorageErrorType }

declare enum StorageFileType {
    File = "File",
    Directory = "Directory",
}

declare type StorageFileMetadata = {
    Name: string
    Type: StorageFileType
    Size: number
}

declare type StorageFile = {
    Metadata: StorageFileMetadata
    Data: Array<number>
}

// @ts-ignore
declare const Storage: {
    Exists: (path: string) => boolean
    List: (path: string) => StorageResponse<StorageFileMetadata[]>
    Read: (path: string) => StorageResponse<StorageFile>
    Write: (path: string, data: Array<number>) => StorageResponse<never>
    Delete: (path: string, recursive: boolean) => StorageResponse<never>
    MakeDirectory: (path: string) => StorageResponse<never>
}
