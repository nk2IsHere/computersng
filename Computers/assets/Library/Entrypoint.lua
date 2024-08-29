-- RGBA color values
local colorRed = {255, 0, 0, 255}
local colorWhite = {255, 255, 255, 255}

function Entry()
    while true do
        Render.Begin()
        Render.Rectangle(0, 0, 20, 20, colorRed)
        Render.Text("Hello from lua!", 0, 0, 20, colorWhite)
        Render.End()
    end
end
