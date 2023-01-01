using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);



var concurrencyPolicy = "Concurrency";
builder.Services.AddRateLimiter(o =>
{
    o.AddConcurrencyLimiter(policyName: concurrencyPolicy, options =>
    {
        options.PermitLimit = 50;
        options.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
        options.QueueLimit = 0;
    });

    o.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/html";
        await context.HttpContext.Response.WriteAsync("<html><body><h1>rejected</h1></body></html>");
    };

});


var app = builder.Build();

app.UseRateLimiter();
app.MapGet("/", async (HttpRequest req, HttpResponse resp) =>
{
    await Task.Delay(50);
    
    
    return Results.Text($"Hello World! {req.Protocol}");
}).RequireRateLimiting(concurrencyPolicy);



app.Run();