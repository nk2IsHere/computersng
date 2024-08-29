using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Computers.Computer.Boundary;

public interface IComputerEvent {
    T Data<T>();
}

public record TickComputerEvent(uint Ticks) : IComputerEvent {
    public T Data<T>() => (T) (object) Ticks;
}

public record RenderComputerEvent(Rectangle Rectangle, SpriteBatch Batch) : IComputerEvent {
    public T Data<T>() => (T) (object) (Rectangle, Batch);
}
