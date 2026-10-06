using dotnet_logging_sample1.Services;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_logging_sample1.Controllers;

[ApiController]
[Route("[controller]")]
public partial class WeatherForecastController(IWeatherService weatherService, ILogger<WeatherForecastController> logger)
    : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public ActionResult<IEnumerable<WeatherForecast>> Get()
    {
        LogRequestReceived();

        try
        {
            var weatherCollection = weatherService.GetWeatherForecast().ToArray();
            LogRequestCompleted(weatherCollection.Length);
            return Ok(weatherCollection);
        }
        catch (InvalidOperationException ex)
        {
            // Log here (with the exception object, not just its message) so
            // the failure is captured with a stack trace before translating
            // it into a clean, client-safe HTTP response.
            LogRequestFailed(ex);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "Weather forecast temporarily unavailable. Please try again.");
        }
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Weather forecast requested")]
    private partial void LogRequestReceived();

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Weather forecast request completed with {Count} result(s)")]
    private partial void LogRequestCompleted(int count);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "Weather forecast request failed")]
    private partial void LogRequestFailed(Exception exception);
}