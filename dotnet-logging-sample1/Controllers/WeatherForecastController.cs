using dotnet_logging_sample1.Services;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_logging_sample1.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(IWeatherService weatherService) : ControllerBase
{
    // No try/catch or request logging here: OpenTelemetry traces every request,
    // and GlobalExceptionHandler handles failures.
    [HttpGet(Name = "GetWeatherForecast")]
    public ActionResult<IEnumerable<WeatherForecast>> Get() => Ok(weatherService.GetWeatherForecast());
}