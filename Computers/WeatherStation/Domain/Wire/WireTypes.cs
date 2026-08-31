using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;

namespace Computers.WeatherStation.Domain.Wire;

public record WeatherReading(
    int Time,
    int Day,
    string Season,
    int Year,
    string Weather,
    string WeatherTomorrow,
    double DailyLuck
);

public record WeatherEvent(string Event, WeatherReading Reading);

public abstract record WeatherRequest;

public record WeatherPingRequest : WeatherRequest;

public record WeatherReadRequest : WeatherRequest;
