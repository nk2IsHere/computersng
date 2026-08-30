using Computers.Router.Domain;
using Computers.WeatherStation.Domain.Wire;

namespace Computers.WeatherStation;

public interface IWeatherWorld {
    WeatherReading? ReadingFor(Placement placement);
}
