using BlockchainHistory.Application.Snapshots.CaptureAll;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlockchainHistory.Api.BackgroundServices;

/// <summary>Settings for the optional scheduled capture.</summary>
public sealed class PollingOptions
{
    public const string SectionName = "Polling";

    public bool Enabled { get; set; }

    public int IntervalSeconds { get; set; } = 300;
}

/// <summary>Captures all chains on a timer, so history is evenly spaced. Off by default; a failed run doesn't stop the next.</summary>
internal sealed partial class SnapshotPollingService(
    IServiceScopeFactory scopes,
    IOptions<PollingOptions> options,
    TimeProvider timeProvider,
    ILogger<SnapshotPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds), timeProvider);
        do
        {
            await CaptureOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CaptureOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            // New scope per run, so each gets a fresh DbContext.
            await using var scope = scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new CaptureAllSnapshotsCommand(), stoppingToken);
            LogTick(result.Captured.Count, result.Failed.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogTickFailed(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Scheduled capture stored {Captured} snapshots, {Failed} failed")]
    private partial void LogTick(int captured, int failed);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduled capture failed; will retry on the next tick")]
    private partial void LogTickFailed(Exception exception);
}

internal static class PollingExtensions
{
    public static IServiceCollection AddSnapshotPolling(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PollingOptions>()
            .Bind(configuration.GetSection(PollingOptions.SectionName))
            .Validate(o => o.IntervalSeconds > 0, $"{PollingOptions.SectionName}:IntervalSeconds must be positive.")
            .ValidateOnStart();

        return services.AddHostedService<SnapshotPollingService>();
    }
}
