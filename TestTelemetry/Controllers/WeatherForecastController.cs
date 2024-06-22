using Microsoft.AspNetCore.Mvc;


namespace TestTelemetry.Controllers;

[ApiController]
[Route("weather/")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly HttpClient _httpClient;
    public WeatherForecastController(ILogger<WeatherForecastController> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    [Route("request")]
    [HttpPost]
    public async Task<IActionResult> Ping(string url)
    {
        var response = await _httpClient.GetAsync(url);
        return Ok(response);
    }
    
    [Route("request/post")]
    [HttpPost]
    public async Task<IActionResult> PingPost(string url)
    {
        var response = await _httpClient.PostAsync(url, null);
        return Ok(response);
    }
    
    [Route("CreateTraces")]
    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> GetRaw()
    {
        await Task.Delay(1000);
        return await CreateForecastData();
    }

    private static async Task DelayWork()
    {   
        using var _ = Tracing.ActivitySource.StartActivity();
        await Task.Delay(1000);
    }

    private static async Task<WeatherForecast[]> CreateForecastData()
    {
        using var _ = Tracing.ActivitySource.StartActivity();
        await Task.Delay(1000);
        var v1 = DelayWork();
        var v2 = DelayWork();
        
        await Task.WhenAll(v1, v2);

        await DelayWork();
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }


    [HttpPost]
    [Route("CreateSomeLogs")]
    public IEnumerable<WeatherForecast> GetByDate()
    {
        _logger.BeginScope("I AM A SCOPE {ID}", Guid.NewGuid());
        _logger.LogInformation("{CURRENTTIME} TEST STRUCTURED LOG", DateTimeOffset.Now);
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}