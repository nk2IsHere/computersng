using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;

namespace Computers.Computer.Boundary;

public interface IComputerEvent {
    T Data<T>();
    bool Global => true;
    bool BelongsTo(string id) => true;
}

public record TickComputerEvent(uint Ticks) : IComputerEvent {
    public T Data<T>() => (T) (object) Ticks;
}

public record RenderComputerEvent(Rectangle Rectangle, SpriteBatch Batch) : IComputerEvent {
    public T Data<T>() => (T) (object) (Rectangle, Batch);
}

public record StopComputerEvent(string? ComputerId) : IComputerEvent {
     
    public T Data<T>() => default!;
    public bool Global => !string.IsNullOrEmpty(ComputerId);
    public bool BelongsTo(string id) => string.IsNullOrEmpty(ComputerId) || ComputerId == id;
}

public record KeyPressedEvent(string ComputerId, Keys Key) : IComputerEvent {
    public T Data<T>() => (T) (object) Key;
    public bool Global => false;
    public bool BelongsTo(string id) => ComputerId == id;
}

public record ButtonHeldEvent(SButton Key) : IComputerEvent {
    public T Data<T>() => (T) (object) Key;
    public bool Global => true;
}

public record ButtonUnheldEvent(SButton Key) : IComputerEvent {
    public T Data<T>() => (T) (object) Key;
    public bool Global => true;
}

public record MouseLeftClickedEvent(string ComputerId, int X, int Y) : IComputerEvent {
    public T Data<T>() => (T) (object) (X, Y);
    public bool Global => false;
    public bool BelongsTo(string id) => ComputerId == id;
}

public record MouseRightClickedEvent(string ComputerId, int X, int Y) : IComputerEvent {
    public T Data<T>() => (T) (object) (X, Y);
    public bool Global => false;
    public bool BelongsTo(string id) => ComputerId == id;
}

public record MouseWheelEvent(string ComputerId, int Direction) : IComputerEvent {
    public T Data<T>() => (T) (object) Direction;
    public bool Global => false;
    public bool BelongsTo(string id) => ComputerId == id;
}
