using System.Text.Json;
using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using Microsoft.Extensions.Options;
using Polly;

namespace BlockchainHistory.Infrastructure.BlockCypher;

/// <summary>Calls BlockCypher. Retries and timeouts come from the pipeline; this class just handles the result.</summary>
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
            // Clients may see this message, so keep the details in the inner exception.
            throw new BlockCypherUnavailableException(network, $"BlockCypher is unavailable for {info.Slug}: {Describe(ex)}", ex);
        }
    }

    private string BuildPath(NetworkInfo info)
    {
        var token = options.Value.Token;
        return string.IsNullOrWhiteSpace(token) ? info.Path : $"{info.Path}?token={Uri.EscapeDataString(token)}";
    }

    // If the caller cancelled, that's not BlockCypher's fault, so let it through.
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
