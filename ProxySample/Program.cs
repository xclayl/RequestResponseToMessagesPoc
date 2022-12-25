using ProxySample;
using ProxySample.ThreadingData;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", WebHandler.Get);
app.MapPost("/", WebHandler.Post);
app.MapGet("/stats", WebHandler.GetStats);

ThreadDataStore.Shutdown = app.Lifetime.ApplicationStopping;

app.Run();