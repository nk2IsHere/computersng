using Computers.PlayerSensor.Domain.Wire;
using Computers.Router.Domain;

namespace Computers.PlayerSensor;

public interface ISensorWorld {
    SensorReading? ReadingFor(Placement placement, int radius);
}
