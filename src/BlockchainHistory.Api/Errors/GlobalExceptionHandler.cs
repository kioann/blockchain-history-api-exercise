using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainHistory.Api.Errors;

/// <summary>Turns exceptions into RFC 9457 problem details, so every error has the same JSON shape.</summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetails, IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => ToValidationProblem(validation),
            BlockCypherUnavailableException unavailable => ToUpstreamProblem(unavailable),
            _ => ToUnexpectedProblem(exception),
        };

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ValidationProblemDetails ToValidationProblem(ValidationException exception) =>
        new(exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        };

    private ProblemDetails ToUpstreamProblem(BlockCypherUnavailableException exception)
    {
        var network = BlockchainNetworks.Get(exception.Network).Slug;
        LogUpstreamUnavailable(exception, network);

        return new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Upstream blockchain provider unavailable",
            Detail = exception.Message,
            Extensions = { ["network"] = network },
        };
    }

    private ProblemDetails ToUnexpectedProblem(Exception exception)
    {
        LogUnexpected(exception);

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = environment.IsDevelopment() ? exception.ToString() : null,
        };
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "BlockCypher unavailable for {Network}")]
    private partial void LogUpstreamUnavailable(Exception exception, string network);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private partial void LogUnexpected(Exception exception);
}
