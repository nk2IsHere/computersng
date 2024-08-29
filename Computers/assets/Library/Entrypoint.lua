-- RGBA color values
local colorRed = {255, 0, 0, 255}
local colorWhite = {255, 255, 255, 255}

local size = 20
function Tick(dt)
    size = (size + 1) % 500
    Render.Clear()
    Render.Rectangle(0, 0, size, size, colorRed)
    Render.Text("Hello from lua!", 0, 0, size, colorWhite)
end
