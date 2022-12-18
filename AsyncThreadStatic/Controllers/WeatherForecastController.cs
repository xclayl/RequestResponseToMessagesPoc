using Microsoft.AspNetCore.Mvc;

namespace AsyncThreadStatic.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static int Counter;
    [ThreadStatic]
    private static int MyThreadId;
    
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
    public async ValueTask<IEnumerable<string>> Get(CancellationToken t)
    {
        var prev = SynchronizationContext.Current;

        try
        {
            var s = new MySynchronizationContext();
            var list = new List<string>();
            
            SynchronizationContext.SetSynchronizationContext(s);

            await Task.Yield();

            if (MyThreadId == 0)
            {
                MyThreadId = Interlocked.Increment(ref Counter);
            }
            
            
            Console.WriteLine($"before async run {Thread.CurrentThread.ManagedThreadId}");

            list.Add($"before async run thread {((SynchronizationContext.Current as MySynchronizationContext)?.MyThreadId.ToString() ?? "null" )} {Thread.CurrentThread.ManagedThreadId}");

            list.Add($"{MyThreadId} {Thread.CurrentThread.ManagedThreadId}");

            await BuildList(list);

            
            Console.WriteLine($"after async run {Thread.CurrentThread.ManagedThreadId}");

            list.Add($"after async run thread {((SynchronizationContext.Current as MySynchronizationContext)?.MyThreadId.ToString() ?? "null" )} {Thread.CurrentThread.ManagedThreadId}");

            list.Add($"{MyThreadId} {Thread.CurrentThread.ManagedThreadId}");
            
            
            SynchronizationContext.SetSynchronizationContext(s);

            await Task.Yield();

            
            
            Console.WriteLine($"after async yield {Thread.CurrentThread.ManagedThreadId}");

            list.Add($"after async yield thread {((SynchronizationContext.Current as MySynchronizationContext)?.MyThreadId.ToString() ?? "null" )} {Thread.CurrentThread.ManagedThreadId}");

            list.Add($"{MyThreadId} {Thread.CurrentThread.ManagedThreadId}");

            return list;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prev);
        }
    }

    private static async Task BuildList(List<string> list)
    {
  
        list.Add($"thread {((SynchronizationContext.Current as MySynchronizationContext)?.MyThreadId.ToString() ?? "null" )} {Thread.CurrentThread.ManagedThreadId}");



        list.Add($"{MyThreadId} {Thread.CurrentThread.ManagedThreadId}");

        Console.WriteLine($"before async a {Thread.CurrentThread.ManagedThreadId}");
        await Task.Delay(500);
        Console.WriteLine($"after async a {Thread.CurrentThread.ManagedThreadId}");

        list.Add($"thread {((SynchronizationContext.Current as MySynchronizationContext)?.MyThreadId.ToString() ?? "null" )} {Thread.CurrentThread.ManagedThreadId}");

        list.Add($"{MyThreadId} {Thread.CurrentThread.ManagedThreadId}");
        
        Console.WriteLine($"before async b {Thread.CurrentThread.ManagedThreadId}");
        await Task.Delay(500);
        Console.WriteLine($"after async b {Thread.CurrentThread.ManagedThreadId}");

        list.Add($"thread {((SynchronizationContext.Current as MySynchronizationContext)?.MyThreadId.ToString() ?? "null" )} {Thread.CurrentThread.ManagedThreadId}");

        list.Add($"{MyThreadId} {Thread.CurrentThread.ManagedThreadId}");

        // if (list.Distinct().Count() > 1)
        //     throw new Exception("changed context");

    }
}