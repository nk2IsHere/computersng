using Computers.Router.Domain;
using Computers.WeatherStation;
using Computers.WeatherStation.Domain.Wire;
using StardewValley;

namespace Computers.Game.Domain;

public class StardewWeatherWorld : IWeatherWorld {
    public WeatherReading? ReadingFor(Placement placement) {
        var location = Game1.getLocationFromName(placement.Location);
        if (location is null) {
            return null;
        }

        var weather = location.GetWeather();
        return new WeatherReading(
            Game1.timeOfDay,
            Game1.dayOfMonth,
            location.GetSeason().ToString().ToLowerInvariant(),
            Game1.year,
            weather.Weather,
            weather.WeatherForTomorrow ?? "",
            Game1.player.DailyLuck
        );
    }
}
