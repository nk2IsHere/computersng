using Microsoft.Xna.Framework.Graphics;

namespace Computers.Computer.Boundary;

public interface IComputerState {
    void Tick(int ticks);
    void Render(SpriteBatch batch);
}
