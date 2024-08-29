-- RGBA color values
local colorRed = {255, 0, 0, 255}
local colorWhite = {255, 255, 255, 255}

function Entry()
    local screenWidth, screenHeight = table.unpack(Render.GetScreenBoundaries())
    local defaultFontSize = Render.GetDefaultFontSize()

    while true do
        local ok, err = pcall(Loop, screenWidth, screenHeight, defaultFontSize)
        if not ok then
            print("Error: " .. err)
        end
    end
end


local keysPressed = {}

function Loop(screenWidth, screenHeight, defaultFontSize)
	local latestEvents = Event.Poll()
    for i = 1, #latestEvents do
        local event = latestEvents[i]
        if event.Type == "KeyPressed" then
            table.insert(keysPressed, event.Data[1])
        end
    end

    Render.Begin()
    Render.Rectangle(0, 0, screenWidth, screenHeight, colorRed)
    Render.Text("Hello from lua! Rendering on " .. screenWidth .. "x" .. screenHeight .. " screen", 0, 0, defaultFontSize, colorWhite)
    Render.Text("Latest keys: " .. table.concat(keysPressed, ", "), 0, defaultFontSize, defaultFontSize, colorWhite)
    Render.End()
end