
---@shape Render
---@field Begin fun():void
---@field End fun():void
---@field Rectangle fun(x: number, y: number, width: number, height: number, color: number[]):void
---@field Text fun(text: string, x: number, y: number, fontSize: number, color: number[]):void
---@field BorderRectangle fun(x: number, y: number, width: number, height: number, borderWidth: number, color: number[]):void
---@field Circle fun(x: number, y: number, radius: number, color: number[]):void
---@field BorderCircle fun(x: number, y: number, radius: number, borderWidth: number, color: number[]):void
---@field Line fun(x1: number, y1: number, x2: number, y2: number, color: number[]):void
---@field ClearBackground fun(color: number[]):void
---@field ClearForeground fun():void
---@field SetBackground fun(x: number, y: number, color: number[]):void
---@field SetForeground fun(x: number, y: number, color: number[]):void
---@field GetScreenBoundaries fun():number[]
---@field GetMaximalFontSize fun():number
---@field GetDefaultFontSize fun():number
---@field MeasureTextWidth fun(text: string, size: number):number[]
Render = {}

---@shape EventEntry
---@field Type string
---@field Data any[]
EventEntry = {}

---@shape Event
---@field Poll fun():EventEntry[]
Event = {}

---@shape System
---@field Sleep fun(ms: number):void
---@field Time fun():string
System = {}

---@shape Color
---@field Red number
---@field Green number
---@field Blue number
---@field Alpha number

---@type
---@field Red Color
---@field Green Color
---@field Blue Color
---@field White Color
---@field Black Color
---@field Yellow Color
---@field Magenta Color
---@field Cyan Color
Colors = {
    ["Red"] = {255, 0, 0, 255},
    ["Green"] = {0, 255, 0, 255},
    ["Blue"] = {0, 0, 255, 255},
    ["White"] = {255, 255, 255, 255},
    ["Black"] = {0, 0, 0, 255},
    ["Yellow"] = {255, 255, 0, 255},
    ["Magenta"] = {255, 0, 255, 255},
    ["Cyan"] = {0, 255, 255, 255},
    ["Transparent"] = {0, 0, 0, 0}
}

function Entry()
    local screenWidth, screenHeight = table.unpack(Render.GetScreenBoundaries())
    local defaultFontSize = Render.GetDefaultFontSize()

    -- Fill the screen with black color
    Render.Begin()
    Render.Rectangle(0, 0, screenWidth, screenHeight, Colors.Black)
    Render.Text("Early initialization", 0, 0, defaultFontSize, Colors.White)
    Render.End()

    -- Main loop
    while true do
        local latestEvents = Event.Poll()
        for _, event in ipairs(latestEvents) do
            if event.Type == "KeyPressed" then
                local key = table.unpack(event.Data)
                if key == 112 then -- F1
                    error("Reload")
                end
            end
        end
        
        local ok, err = pcall(Loop, screenWidth, screenHeight, defaultFontSize, latestEvents)
        if not ok then
            print("Error: " .. err)
        end
    end
end

function Loop(screenWidth, screenHeight, defaultFontSize, latestEvents)
    local charCount = math.ceil(screenWidth / defaultFontSize)
    local fullWidthChar = string.rep("=", charCount)
    
    Render.Begin()
    Render.Text("Hello from lua! Rendering on " .. screenWidth .. "x" .. screenHeight, 0, 0, defaultFontSize, Colors.White)
    Render.Text("Current time: " .. System.Time(), 0, defaultFontSize, defaultFontSize, Colors.White)
    Render.Text(fullWidthChar, 0, defaultFontSize * 2, defaultFontSize, Colors.White)
    Render.End()
end
