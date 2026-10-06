namespace dotnet_logging_sample1.Services;

public interface IWeatherService
{
    IEnumerable<WeatherForecast> GetWeatherForecast();
}

public partial class WeatherService(ILogger<WeatherService> logger) : IWeatherService
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public IEnumerable<WeatherForecast> GetWeatherForecast()
    {
        const int days = 5;
        LogGeneratingForecast(days);

        var weatherCollection = Enumerable.Range(1, days).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();

        foreach (var forecast in weatherCollection.Where(f => f.TemperatureC is <= -15 or >= 50))
        {
            LogExtremeTemperature(forecast.Date, forecast.TemperatureC);
        }

        LogForecastGenerated(weatherCollection.Length);
        return weatherCollection;
    }

    // Source-generated logging: zero-allocation on the hot path, strongly-typed
    // parameters, and consistent EventIds for filtering/alerting downstream.
    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Generating weather forecast for {Days} day(s)")]
    private partial void LogGeneratingForecast(int days);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Generated {Count} weather forecast entries")]
    private partial void LogForecastGenerated(int count);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Extreme temperature forecast for {Date}: {TemperatureC}\u00b0C")]
    private partial void LogExtremeTemperature(DateOnly date, int temperatureC);
}