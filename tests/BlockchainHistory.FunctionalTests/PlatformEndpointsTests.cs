using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlockchainHistory.FunctionalTests;

public sealed class PlatformEndpointsTests : IClassFixture<BlockchainHistoryApiFactory>
{
    private readonly HttpClient _client;

    public PlatformEndpointsTests(BlockchainHistoryApiFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_ReportHealthy(string url)
    {
        var response = await _client.GetAsync(url, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ReadinessCheck_IncludesTheDatabase()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/health/ready", Ct);

        Assert.Contains(body.GetProperty("checks").EnumerateArray(), c => c.GetProperty("name").GetString() == "database");
    }

    [Fact]
    public async Task OpenApiDocument_DescribesEverySnapshotRoute()
    {
        var document = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json", Ct);

        var paths = document.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/api/v1/blockchains", paths);
        Assert.Contains("/api/v1/blockchains/snapshots", paths);
        Assert.Contains("/api/v1/blockchains/{network}/snapshots", paths);
        Assert.Contains("/api/v1/snapshots/{id}", paths);
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        var response = await _client.GetAsync("/swagger/index.html", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CorsPreflight_FromAllowedOrigin_IsAllowed()
    {
        using var response = await SendPreflightAsync(BlockchainHistoryApiFactory.AllowedOrigin);

        Assert.Equal(
            BlockchainHistoryApiFactory.AllowedOrigin,
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task CorsPreflight_FromUnknownOrigin_IsNotAllowed()
    {
        using var response = await SendPreflightAsync("https://evil.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private async Task<HttpResponseMessage> SendPreflightAsync(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/blockchains/btc-main/snapshots");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return await _client.SendAsync(request, Ct);
    }
}
