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

        Assert.Equal(TestChainStatus.Ethereum(), result.Status);
        Assert.Equal(body, result.RawJson);
    }

    [Fact]
    public async Task GetChain_Btc_ParsesFeePerKb()
    {
        Respond("/btc/main", 200, await File.ReadAllTextAsync(Path.Combine("Fixtures", "btc-main.json"), Ct));

        var result = await CreateClient().GetChainAsync(BlockchainNetwork.BtcMain, Ct);

        Assert.Equal(TestChainStatus.Utxo(), result.Status);
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

    [Fact]
    public async Task GetChain_NullInARequiredField_ThrowsUnavailable()
    {
        Respond("/btc/main", 200, MinimalBody("BTC.main").Replace("\"hash\":\"h\"", "\"hash\":null", StringComparison.Ordinal));

        await Assert.ThrowsAsync<BlockCypherUnavailableException>(
            () => CreateClient().GetChainAsync(BlockchainNetwork.BtcMain, Ct));
    }

    [Fact]
    public async Task GetChain_HourlyBudgetUsedUp_FailsWithoutCallingBlockCypher()
    {
        Respond("/btc/main", 200, MinimalBody("BTC.main"));
        var client = CreateClient(requestsPerHour: 2);

        await client.GetChainAsync(BlockchainNetwork.BtcMain, Ct);
        await client.GetChainAsync(BlockchainNetwork.BtcMain, Ct);
        await Assert.ThrowsAsync<BlockCypherUnavailableException>(() => client.GetChainAsync(BlockchainNetwork.BtcMain, Ct));

        Assert.Equal(2, _server.LogEntries.Count);
    }

    [Fact]
    public async Task GetChain_BurstAboveThePerSecondLimit_IsQueuedNotRejected()
    {
        Respond("/btc/main", 200, MinimalBody("BTC.main"));
        // Warm the fake server up first: while it starts it queues requests and stamps them all at once.
        using (var warmUp = new HttpClient())
        {
            await warmUp.GetAsync(new Uri(_server.Url + "/btc/main"), Ct);
        }

        _server.ResetLogEntries();
        var client = CreateClient(requestsPerSecond: 2);

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.GetChainAsync(BlockchainNetwork.BtcMain, Ct)));

        // All five arrive, but spread over time: only two may be sent in the first second.
        var arrivals = _server.LogEntries.Select(e => e.RequestMessage!.DateTime).Order().ToList();
        Assert.Equal(5, arrivals.Count);
        Assert.True(
            arrivals[^1] - arrivals[0] >= TimeSpan.FromMilliseconds(900),
            "requests beyond 2/s should wait; arrivals: " + string.Join(", ", arrivals.Select(a => a.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture))));
    }

    private void Respond(string path, int status, string body) =>
        _server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(status).WithBody(body));

    private static string MinimalBody(string name) =>
        $$"""
        {"name":"{{name}}","height":1,"hash":"h","time":"2026-10-16T00:00:00Z","latest_url":"u",
         "previous_hash":"p","previous_url":"pu","peer_count":1,"unconfirmed_count":0}
        """;

    private IBlockCypherClient CreateClient(string? token = null, int requestsPerSecond = 100, int requestsPerHour = 1000)
    {
        var settings = new Dictionary<string, string?>
        {
            ["BlockCypher:BaseUrl"] = _server.Url + "/",
            ["BlockCypher:Token"] = token,
            ["BlockCypher:RetryCount"] = "2",
            ["BlockCypher:RetryDelayMilliseconds"] = "10",
            ["BlockCypher:TimeoutSeconds"] = "5",
            ["BlockCypher:RequestsPerSecond"] = requestsPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["BlockCypher:RequestsPerHour"] = requestsPerHour.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddLogging()
            .AddBlockCypherClient(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IBlockCypherClient>();
    }
}
