using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Application.Common;
using BlockchainHistory.Application.Snapshots;
using BlockchainHistory.Application.Snapshots.CaptureAll;
using BlockchainHistory.Domain.Chains;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BlockchainHistory.FunctionalTests;

public sealed class SnapshotEndpointsTests : IDisposable
{
    private readonly BlockchainHistoryApiFactory _factory = new();
    private readonly HttpClient _client;

    public SnapshotEndpointsTests() => _client = _factory.CreateClient();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task ListBlockchains_ReturnsTheFiveSupportedNetworks()
    {
        var networks = await _client.GetFromJsonAsync<JsonElement>("/api/v1/blockchains", Ct);

        Assert.Equal(
            ["eth-main", "dash-main", "btc-main", "btc-test3", "ltc-main"],
            networks.EnumerateArray().Select(n => n.GetProperty("slug").GetString()));
    }

    [Fact]
    public async Task ListBlockchains_SourceUrlsUseTheConfiguredBaseUrl()
    {
        var networks = await _client.GetFromJsonAsync<JsonElement>("/api/v1/blockchains", Ct);

        var btc = networks.EnumerateArray().Single(n => n.GetProperty("slug").GetString() == "btc-test3");
        Assert.Equal(BlockchainHistoryApiFactory.BlockCypherBaseUrl + "btc/test3", btc.GetProperty("sourceUrl").GetString());
    }

    [Fact]
    public async Task CaptureAll_EveryNetworkDown_Returns503()
    {
        _factory.BlockCypher.GetChainAsync(Arg.Any<BlockchainNetwork>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(call => new BlockCypherUnavailableException(call.Arg<BlockchainNetwork>(), "down"));

        var response = await _client.PostAsync("/api/v1/blockchains/snapshots", null, Ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Capture_UpstreamDown_DoesNotExposeInternalErrorText()
    {
        _factory.BlockCypher.GetChainAsync(BlockchainNetwork.BtcMain, Arg.Any<CancellationToken>())
            .ThrowsAsync(new BlockCypherUnavailableException(BlockchainNetwork.BtcMain, "No such host: internal-dns-10.0.0.7"));

        var response = await _client.PostAsync("/api/v1/blockchains/btc-main/snapshots", null, Ct);

        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("internal-dns", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostSnapshot_Returns201WithLocationOfTheNewSnapshot()
    {
        var response = await _client.PostAsync("/api/v1/blockchains/btc-main/snapshots", null, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SnapshotDto>(Ct);
        Assert.NotNull(created);
        Assert.Equal("btc-main", created.Network);
        Assert.Equal(_factory.Clock.GetUtcNow(), created.CreatedAt);
        Assert.Equal(1000L, created.UtxoFees!.LowFeePerKb);
        Assert.Equal("BTC.main", created.Raw.GetProperty("name").GetString());

        var fetched = await _client.GetFromJsonAsync<SnapshotDto>(response.Headers.Location, Ct);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task History_ReturnsNewestFirst()
    {
        for (var i = 0; i < 3; i++)
        {
            await _client.PostAsync("/api/v1/blockchains/eth-main/snapshots", null, Ct);
            _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        }

        var page = await GetHistoryAsync("/api/v1/blockchains/eth-main/snapshots");

        var times = page.Items.Select(i => i.CreatedAt).ToList();
        Assert.Equal(3, times.Count);
        Assert.Equal(times.OrderByDescending(t => t), times);
        Assert.Equal(3, page.TotalCount);
    }

    [Fact]
    public async Task History_OnlyContainsTheRequestedNetwork()
    {
        await _client.PostAsync("/api/v1/blockchains/btc-main/snapshots", null, Ct);
        await _client.PostAsync("/api/v1/blockchains/ltc-main/snapshots", null, Ct);

        var page = await GetHistoryAsync("/api/v1/blockchains/ltc-main/snapshots");

        Assert.Equal("ltc-main", Assert.Single(page.Items).Network);
    }

    [Fact]
    public async Task History_UppercaseSlug_IsAccepted()
    {
        var response = await _client.GetAsync("/api/v1/blockchains/BTC-Main/snapshots", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task History_UnknownChain_Returns400ListingSupportedChains()
    {
        var response = await _client.GetAsync("/api/v1/blockchains/doge-main/snapshots", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.Contains("doge-main", body, StringComparison.Ordinal);
        Assert.Contains("eth-main, dash-main, btc-main, btc-test3, ltc-main", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=0")]
    public async Task History_InvalidPaging_Returns400(string query)
    {
        var response = await _client.GetAsync($"/api/v1/blockchains/btc-main/snapshots?{query}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task History_PageBeyondTheEnd_ReturnsEmptyItemsWithTotal()
    {
        await _client.PostAsync("/api/v1/blockchains/btc-main/snapshots", null, Ct);

        var page = await GetHistoryAsync("/api/v1/blockchains/btc-main/snapshots?page=9&pageSize=10");

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task CaptureAll_CapturesEveryNetwork()
    {
        var response = await _client.PostAsync("/api/v1/blockchains/snapshots", null, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CaptureAllResult>(Ct);
        Assert.Equal(5, result!.Captured.Count);
        Assert.Empty(result.Failed);
    }

    [Fact]
    public async Task Capture_UpstreamDown_Returns503ProblemDetails()
    {
        _factory.BlockCypher.GetChainAsync(BlockchainNetwork.DashMain, Arg.Any<CancellationToken>())
            .ThrowsAsync(new BlockCypherUnavailableException(BlockchainNetwork.DashMain, "BlockCypher returned 503"));

        var response = await _client.PostAsync("/api/v1/blockchains/dash-main/snapshots", null, Ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Upstream blockchain provider unavailable", problem.GetProperty("title").GetString());
        Assert.Equal("dash-main", problem.GetProperty("network").GetString());
    }

    [Fact]
    public async Task ConcurrentCaptures_OfTheSameChain_AreBothStored()
    {
        var responses = await Task.WhenAll(
            _client.PostAsync("/api/v1/blockchains/btc-main/snapshots", null, Ct),
            _client.PostAsync("/api/v1/blockchains/btc-main/snapshots", null, Ct));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        var page = await GetHistoryAsync("/api/v1/blockchains/btc-main/snapshots");
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetById_Unknown_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"/api/v1/snapshots/{Guid.NewGuid()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Capture_OverTheHourlyBudget_Returns429()
    {
        using var factory = new BlockchainHistoryApiFactory { CapturePermitsPerHour = 1 };
        using var client = factory.CreateClient();

        var first = await client.PostAsync("/api/v1/blockchains/btc-main/snapshots", null, Ct);
        var second = await client.PostAsync("/api/v1/blockchains/btc-main/snapshots", null, Ct);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    private async Task<PagedResult<SnapshotDto>> GetHistoryAsync(string url) =>
        (await _client.GetFromJsonAsync<PagedResult<SnapshotDto>>(url, Ct))!;
}
