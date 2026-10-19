using BlockchainHistory.Api.Extensions;
using BlockchainHistory.Application;
using BlockchainHistory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);
builder.AddObservability();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseApiDocumentation();
app.UseCors(CorsExtensions.PolicyName);
app.UseRateLimiter();

app.MapControllers();
app.MapHealthEndpoints();

await app.Services.MigrateDatabaseAsync();
await app.RunAsync();

/// <summary>Public so tests can host the API.</summary>
public partial class Program;
