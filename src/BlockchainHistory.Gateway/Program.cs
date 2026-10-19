// API gateway: a single public entry point that forwards to the backend API.
// Routes and destinations live in configuration (ReverseProxy section), so new services
// can be added behind the same front door without code changes.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/live");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapReverseProxy();

await app.RunAsync();
