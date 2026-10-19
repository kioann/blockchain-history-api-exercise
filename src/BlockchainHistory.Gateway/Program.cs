// API gateway: one front door for the API. Routes live in config, so adding a service needs no code.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/live");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapReverseProxy();

await app.RunAsync();
