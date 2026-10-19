using System.Diagnostics;
using BlockchainHistory.Application.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BlockchainHistory.Application.Common;

/// <summary>Traces every request and logs how long it took.</summary>
public sealed partial class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var name = typeof(TMessage).Name;
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(name);
        var started = Stopwatch.GetTimestamp();
        try
        {
            var response = await next(message, cancellationToken);
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            LogHandled(name, elapsedMs);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            LogFailed(ex, name, elapsedMs);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {MessageName} in {ElapsedMs:0.0} ms")]
    private partial void LogHandled(string messageName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed {MessageName} after {ElapsedMs:0.0} ms")]
    private partial void LogFailed(Exception exception, string messageName, double elapsedMs);
}
