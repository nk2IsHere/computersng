
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
    // The background/foreground pixel layers are PERSISTENT overlays: they survive across
    // frames (Begin only resets the command list) until cleared explicitly. The frame is
    // composed as background -> commands -> foreground (alpha-blended on top).
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
    /** @deprecated Blocks a shared worker (clamped to 100ms). Prefer Delay or NextFrame. */
    Sleep: (ms: number) => void
    Delay: (ms: number) => Promise<void>
    // Pacing primitive: resolves on the next scheduler pulse (a game tick, subject to
    // render.maxFps). Long computations MUST await this (or Delay) periodically - a script
    // that runs too many statements in one stretch is aborted by the engine watchdog.
    NextFrame: () => Promise<void>
    Time: () => number
    LoadModule: <T extends { [key: string]: any }>(path: string) => T
    ProcessTasks: () => void
    Id: () => string
    // OS clipboard access. GetClipboard returns "" when the clipboard is empty or unavailable.
    GetClipboard: () => string
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

declare const Network: {
    RequestHttpBytes: (url: string, method: string, headers?: { [key: string]: string }, body?: Array<number>) => Promise<{ Result: HttpResponseBytes }>
    RequestHttpString: (url: string, method: string, headers?: { [key: string]: string }, body?: string) => Promise<{ Result: HttpResponseString }>

    // LAN messaging over the router mesh. UDP semantics: SendMessage is fire-and-forget with
    // no delivery guarantee; unreachable targets fail silently (TTL death). The payload is an
    // opaque string - JSON by convention (see SendJson in Core/Network). Incoming messages
    // arrive as "NetworkMessage" events via Event.Poll with data [sourceAddress, payload].
    // Throws if this computer has no covering router (offline) or the payload is too large.
    SendMessage: (address: string, payload: string) => void
    GetAddress: () => string
    ListReachable: () => string[]
    GetRouters: () => { address: string, channel: number | null }[]
    ConfigureRouter: (routerAddress: string, channel: number | null) => void
}
