using Microsoft.AspNetCore.Mvc;

namespace RequestToMessagePlayground.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async ValueTask<IEnumerable<WeatherForecast>> Get()
    {
        var loc = RequestQueue.AcquireSlot();
        try
        {
            if (loc.IsValid)
            {
                if (Worker.Enqueue(loc))
                {
                    var workResult = RequestQueue.Get(loc).Reader.ReadAsync().AsTask().Result;


                    return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                        {
                            Date = DateTime.Now.AddDays(index),
                            TemperatureC = Random.Shared.Next(-20, 55),
                            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                        })
                        .ToArray();
                }

            }
        }
        finally
        {
            loc.Free();
        }

        return new WeatherForecast[0];
    }
}