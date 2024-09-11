using Computers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;

namespace Computers.Computer;

public interface IComputerEvent {
    T Data<T>();
    bool Global => true;
    bool BelongsTo(Id id) => true;
}

public record TickComputerEvent(uint Ticks) : IComputerEvent {
    public T Data<T>() => (T) (object) Ticks;
}

public record RenderComputerEvent(Rectangle Rectangle, SpriteBatch Batch) : IComputerEvent {
    public T Data<T>() => (T) (object) (Rectangle, Batch);
}

public record StopComputerEvent(Id? ComputerId = null) : IComputerEvent {
     
    public T Data<T>() => default!;
    public bool Global => ComputerId is null;
    public bool BelongsTo(Id id) => ComputerId is null || ComputerId == id;
}

public record KeyPressedEvent(Id ComputerId, Keys Key) : IComputerEvent {
    public T Data<T>() => (T) (object) Key;
    public bool Global => false;
    public bool BelongsTo(Id id) => ComputerId == id;
}

public record ButtonHeldEvent(SButton Key) : IComputerEvent {
    public T Data<T>() => (T) (object) Key;
    public bool Global => true;
}

public record ButtonUnheldEvent(SButton Key) : IComputerEvent {
    public T Data<T>() => (T) (object) Key;
    public bool Global => true;
}

public record MouseLeftClickedEvent(Id ComputerId, int X, int Y) : IComputerEvent {
    public T Data<T>() => (T) (object) (X, Y);
    public bool Global => false;
    public bool BelongsTo(Id id) => ComputerId == id;
}

public record MouseRightClickedEvent(Id ComputerId, int X, int Y) : IComputerEvent {
    public T Data<T>() => (T) (object) (X, Y);
    public bool Global => false;
    public bool BelongsTo(Id id) => ComputerId == id;
}

public record MouseWheelEvent(Id ComputerId, int Direction) : IComputerEvent {
    public T Data<T>() => (T) (object) Direction;
    public bool Global => false;
    public bool BelongsTo(Id id) => ComputerId == id;
}

public record StartComputerEvent(Id? ComputerId = null) : IComputerEvent {
     
    public T Data<T>() => default!;
    public bool Global => ComputerId is null;
    public bool BelongsTo(Id id) => ComputerId is null || ComputerId == id;
}
