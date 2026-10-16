using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace BlockchainHistory.IntegrationTests.BlockCypher;

/// <summary>
/// Exercises the real HTTP client, including its resilience pipeline, against a local fake BlockCypher server.
/// </summary>
public sealed class BlockCypherClientTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _server.Stop();

    [Fact]
    public async Task GetChain_Eth_ParsesGasFieldsAndKeepsRawBody()
    {
        var body = await File.ReadAllTextAsync(Path.Combine("Fixtures", "eth-main.json"), Ct);
        Respond("/eth/main", 200, body);

        var result = await CreateClient().GetChainAsync(BlockchainNetwork.EthMain, Ct);

        Assert.Equal(TestData.Ethereum(), result.Status);
        Assert.Equal(body, result.RawJson);
    }

    [Fact]
    public async Task GetChain_Btc_ParsesFeePerKb()
    {
        Respond("/btc/main", 200, await File.ReadAllTextAsync(Path.Combine("Fixtures", "btc-main.json"), Ct));

        var result = await CreateClient().GetChainAsync(BlockchainNetwork.BtcMain, Ct);

        Assert.Equal(TestData.Utxo(), result.Status);
    }

    [Fact]
    public async Task GetChain_MissingOptionalFields_ReturnsNulls()
    {
        const string body = """
            {"name":"LTC.main","height":1,"hash":"h","time":"2026-10-16T00:00:00Z","latest_url":"u",
             "previous_hash":"p","previous_url":"pu","peer_count":1,"unconfirmed_count":0}
            """;
        Respond("/ltc/main", 200, body);

        var result = await CreateClient().GetChainAsync(BlockchainNetwork.LtcMain, Ct);

        Assert.Null(result.Status.HighFeePerKb);
        Assert.Null(result.Status.LastForkHash);
        Assert.Null(result.Status.LastForkHeight);
    }

    [Fact]
    public async Task GetChain_WithToken_SendsTokenAsQueryParameter()
    {
        _server.Given(Request.Create().WithPath("/dash/main").WithParam("token", "abc123").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(MinimalBody("DASH.main")));

        var result = await CreateClient(token: "abc123").GetChainAsync(BlockchainNetwork.DashMain, Ct);

        Assert.Equal("DASH.main", result.Status.Name);
    }

    [Fact]
    public async Task GetChain_TransientFailureThenSuccess_RetriesAndSucceeds()
    {
        _server.Given(Request.Create().WithPath("/btc/test3").UsingGet())
            .InScenario("flaky").WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(503));
        _server.Given(Request.Create().WithPath("/btc/test3").UsingGet())
            .InScenario("flaky").WhenStateIs("recovered")
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(MinimalBody("BTC.test3")));

        var result = await CreateClient().GetChainAsync(BlockchainNetwork.BtcTest3, Ct);

        Assert.Equal("BTC.test3", result.Status.Name);
        Assert.Equal(2, _server.LogEntries.Count);
    }

    [Theory]
    [InlineData(503)]
    [InlineData(429)]
    public async Task GetChain_PersistentFailure_ThrowsUnavailableAfterRetries(int status)
    {
        Respond("/btc/main", status, "");

        var ex = await Assert.ThrowsAsync<BlockCypherUnavailableException>(
            () => CreateClient().GetChainAsync(BlockchainNetwork.BtcMain, Ct));

        Assert.Equal(BlockchainNetwork.BtcMain, ex.Network);
        Assert.Contains(status.ToString(System.Globalization.CultureInfo.InvariantCulture), ex.Message, StringComparison.Ordinal);
        Assert.Equal(3, _server.LogEntries.Count);
    }

    [Fact]
    public async Task GetChain_NotFound_ThrowsUnavailableWithoutRetrying()
    {
        Respond("/btc/main", 404, "");

        await Assert.ThrowsAsync<BlockCypherUnavailableException>(
            () => CreateClient().GetChainAsync(BlockchainNetwork.BtcMain, Ct));

        Assert.Single(_server.LogEntries);
    }

    [Fact]
    public async Task GetChain_MalformedJson_ThrowsUnavailable()
    {
        Respond("/eth/main", 200, "<html>maintenance</html>");

        await Assert.ThrowsAsync<BlockCypherUnavailableException>(
            () => CreateClient().GetChainAsync(BlockchainNetwork.EthMain, Ct));
    }

    private void Respond(string path, int status, string body) =>
        _server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(status).WithBody(body));

    private static string MinimalBody(string name) =>
        $$"""
        {"name":"{{name}}","height":1,"hash":"h","time":"2026-10-16T00:00:00Z","latest_url":"u",
         "previous_hash":"p","previous_url":"pu","peer_count":1,"unconfirmed_count":0}
        """;

    private IBlockCypherClient CreateClient(string? token = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["BlockCypher:BaseUrl"] = _server.Url + "/",
            ["BlockCypher:Token"] = token,
            ["BlockCypher:RetryCount"] = "2",
            ["BlockCypher:RetryDelayMilliseconds"] = "10",
            ["BlockCypher:TimeoutSeconds"] = "5",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddLogging()
            .AddBlockCypherClient(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IBlockCypherClient>();
    }
}
