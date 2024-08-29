local size = 20

function Tick(dt)
    size = (size + 1) % 500
    Render.Clear()
    Render.Rectangle(0, 0, size, size, 255, 0, 0)
    Render.Text("Hello from the library!", 0, 0, size, 255, 255, 255)
end
