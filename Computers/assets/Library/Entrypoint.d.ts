
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
        | "NetworkMessage"
    Data: any[]
}

// @ts-ignore
declare const Event: {
    Poll: () => EventData[]
}

declare const System: {
    Delay: (ms: number) => Promise<void>
    NextFrame: () => Promise<void>
    Time: () => number
    LoadModule: <T extends { [key: string]: any }>(path: string) => T
    ProcessTasks: () => void
    Id: () => string
    GetClipboard: () => string | null
    SetClipboard: (text: string) => void
}

declare enum StorageErrorType {
    FileNotFound,
    DirectoryNotFound,
    FileAlreadyExists,
    DirectoryAlreadyExists,
    DirectoryNotEmpty,
    PathIsNotDirectory,
    PathIsNotFile,
    ReadOnlyStorage
}

declare enum StorageResponseType {
    Success,
    Error
}

declare type StorageResponse<T> = 
    | { Type: StorageResponseType.Success, Data: T }
    | { Type: StorageResponseType.Error, Error: StorageErrorType }

declare enum StorageFileType {
    File,
    Directory
}

declare type StorageFileMetadata = {
    Name: string
    Type: StorageFileType
    Size: number
    Layer: string
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
    ReadMetadata: (path: string) => StorageResponse<StorageFileMetadata>
    Write: (path: string, data: Array<number>) => StorageResponse<never>
    Delete: (path: string, recursive: boolean) => StorageResponse<never>
    MakeDirectory: (path: string) => StorageResponse<never>
}

declare type HttpResponseBytes = {
    StatusCode: number
    Headers: { [key: string]: string }
    Body: Array<number>
}

declare type HttpResponseString = {
    StatusCode: number
    Headers: { [key: string]: string }
    Body: string
}

declare type MachineMemberSnapshot = {
    kind: "machine"
    x: number
    y: number
    itemId: string
    name: string
    state: "empty" | "working" | "ready"
    heldItem: string | null
    minutesUntilReady: number
}

declare type ChestMemberSnapshot = {
    kind: "chest"
    x: number
    y: number
    items: { id: string, name: string, count: number }[]
}

declare type ConnectorMemberSnapshot = {
    kind: "connector"
    x: number
    y: number
}

declare type GroupMemberSnapshot = MachineMemberSnapshot | ChestMemberSnapshot | ConnectorMemberSnapshot

declare type GroupSnapshot = {
    truncated: boolean
    members: GroupMemberSnapshot[]
}

declare const Network: {
    RequestHttpBytes: (url: string, method: string, headers?: { [key: string]: string }, body?: Array<number>) => Promise<HttpResponseBytes>
    RequestHttpString: (url: string, method: string, headers?: { [key: string]: string }, body?: string) => Promise<HttpResponseString>
    SendMessage: (address: string, payload: string) => void
    GetAddress: () => string
    GetRouters: () => { address: string, channel: number | null }[]
}
