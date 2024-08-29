-- RGBA color values
local colorRed = {255, 0, 0, 255}
local colorWhite = {255, 255, 255, 255}

function Entry()
    local screenWidth, screenHeight = table.unpack(Render.GetScreenBoundaries())
    local defaultFontSize = Render.GetDefaultFontSize()

    while true do
        Render.Begin()
        Render.Rectangle(0, 0, screenWidth, screenHeight, colorRed)
        Render.Text("Hello from lua! Rendering on " .. screenWidth .. "x" .. screenHeight .. " screen", 0, 0, defaultFontSize, colorWhite)
        Render.End()
    end
end
