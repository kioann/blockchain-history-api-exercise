using System.Text.Json;
using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using Microsoft.Extensions.Options;
using Polly;

namespace BlockchainHistory.Infrastructure.BlockCypher;

/// <summary>
/// Typed HTTP client for BlockCypher. Retries, timeouts and the circuit breaker are applied by the
/// resilience pipeline registered with this client, so this class only maps outcomes.
/// </summary>
internal sealed class BlockCypherClient(HttpClient http, IOptions<BlockCypherOptions> options) : IBlockCypherClient
{
    public async Task<ChainFetchResult> GetChainAsync(BlockchainNetwork network, CancellationToken cancellationToken)
    {
        var info = BlockchainNetworks.Get(network);
        try
        {
            using var response = await http.GetAsync(BuildPath(info), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new BlockCypherUnavailableException(
                    network, $"BlockCypher returned {(int)response.StatusCode} {response.ReasonPhrase} for {info.Slug}.");
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonSerializer.Deserialize(raw, BlockCypherJsonContext.Default.BlockCypherChainResponse)
                ?? throw new JsonException("Empty response body.");

            return new ChainFetchResult(parsed.ToChainStatus(), raw);
        }
        catch (Exception ex) when (IsProviderFailure(ex, cancellationToken))
        {
            // The message may reach API clients, so it names the kind of failure; the details stay in the inner exception.
            throw new BlockCypherUnavailableException(network, $"BlockCypher is unavailable for {info.Slug}: {Describe(ex)}", ex);
        }
    }

    private string BuildPath(NetworkInfo info)
    {
        var token = options.Value.Token;
        return string.IsNullOrWhiteSpace(token) ? info.Path : $"{info.Path}?token={Uri.EscapeDataString(token)}";
    }

    // Caller cancellation is not a provider failure, so it propagates unchanged.
    private static bool IsProviderFailure(Exception ex, CancellationToken cancellationToken) => ex switch
    {
        HttpRequestException or JsonException or ExecutionRejectedException => true,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false,
    };

    private static string Describe(Exception ex) => ex switch
    {
        ExecutionRejectedException => ex.Message,
        JsonException => "unexpected response format.",
        HttpRequestException => "could not connect.",
        _ => "the request timed out.",
    };
}
