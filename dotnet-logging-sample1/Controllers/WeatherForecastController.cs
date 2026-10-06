using dotnet_logging_sample1.Services;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_logging_sample1.Controllers;

[ApiController]
[Route("[controller]")]
public partial class WeatherForecastController(IWeatherService weatherService, ILogger<WeatherForecastController> logger)
    : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        LogRequestReceived();
        var weatherCollection = weatherService.GetWeatherForecast().ToArray();
        LogRequestCompleted(weatherCollection.Length);
        return weatherCollection;
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Weather forecast requested")]
    private partial void LogRequestReceived();

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Weather forecast request completed with {Count} result(s)")]
    private partial void LogRequestCompleted(int count);
}