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

/// <summary>Entry point. Public so functional tests can host the API in memory.</summary>
public partial class Program;
