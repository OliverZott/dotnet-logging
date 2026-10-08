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

        // Simulate an occasional upstream failure (~1 in 5 calls). No logging
        // here - GlobalExceptionHandler logs it once and returns a 503.
        if (Random.Shared.Next(5) == 0)
        {
            throw new InvalidOperationException("Simulated weather provider outage.");
        }

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

        return weatherCollection;
    }

    // Source-generated structured logging: {Placeholders} become queryable
    // fields in Seq/Aspire.
    [LoggerMessage(Level = LogLevel.Warning, Message = "Extreme temperature forecast for {Date}: {TemperatureC}\u00b0C")]
    private partial void LogExtremeTemperature(DateOnly date, int temperatureC);
}