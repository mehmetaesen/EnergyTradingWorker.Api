using System.Net;
using System.Text;
using EnergyTrading.Application;
using EnergyTrading.Infrastructure;
using Microsoft.Extensions.Options;

namespace EnergyTrading.Tests;

public sealed class EpiasClientTests
{
    [Fact]
    public async Task Authentication_success_returns_and_caches_ticket()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("TGT-valid") });
        var client = CreateAuth(handler);
        Assert.Equal("TGT-valid", await client.GetTicketAsync(false, default));
        Assert.Equal("TGT-valid", await client.GetTicketAsync(false, default));
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Authentication_failure_throws_safe_exception()
    {
        var client = CreateAuth(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var error = await Assert.ThrowsAsync<TransparencyAuthenticationException>(() => client.GetTicketAsync(false, default));
        Assert.DoesNotContain("secret", error.Message);
    }

    [Fact]
    public async Task Mcp_response_is_deserialized_and_TGT_header_is_sent()
    {
        HttpRequestMessage? captured = null;
        var json = """{"items":[{"date":"2026-08-17T00:00:00+03:00","hour":"00:00-01:00","price":100.25,"priceUsd":3.1,"priceEur":2.7}]}""";
        var handler = new StubHandler(r => { captured = r; return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") }; });
        var genericClient = new TransparencyHttpClient(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") }, new StubAuthentication());
        var client = new TransparencyApiClient(genericClient);
        var result = await client.GetMcpAsync(new McpRequest(DateTimeOffset.Now, DateTimeOffset.Now), default);
        Assert.Single(result.Items); Assert.Equal(100.25m, result.Items[0].Price); Assert.Equal("TGT-test", captured!.Headers.GetValues("TGT").Single());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"Şeffaflık servisi geçici olarak kullanılamıyor\"")]
    [InlineData("not-json")]
    public async Task Response_that_cannot_be_deserialized_preserves_raw_body(string responseBody)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });
        var client = new TransparencyHttpClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") },
            new StubAuthentication());

        var error = await Assert.ThrowsAsync<TransparencyResponseDeserializationException>(() =>
            client.PostAsync<McpRequest, McpResponse>(
                "v1/markets/dam/data/mcp",
                new McpRequest(DateTimeOffset.Now, DateTimeOffset.Now),
                default));

        Assert.Equal(responseBody, error.ResponseBody);
        Assert.Equal(HttpStatusCode.OK, error.StatusCode);
    }

    [Fact]
    public async Task Unsuccessful_response_preserves_raw_body()
    {
        const string responseBody = """{"errors":[{"errorCode":"E42","errorMessage":"Geçici hata"}]}""";
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });
        var client = new TransparencyHttpClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") },
            new StubAuthentication());

        var error = await Assert.ThrowsAsync<TransparencyApiException>(() =>
            client.PostAsync<McpRequest, McpResponse>(
                "v1/markets/dam/data/mcp",
                new McpRequest(DateTimeOffset.Now, DateTimeOffset.Now),
                default));

        Assert.Equal(responseBody, error.ResponseBody);
        Assert.Equal(HttpStatusCode.BadGateway, error.StatusCode);
    }

    [Fact]
    public async Task Wind_generation_uses_list_endpoint_and_deserializes_response()
    {
        HttpRequestMessage? captured = null;
        const string json = """{"items":[{"date":"2026-08-18T00:00:00+03:00","time":"2026-08-18T01:10:00+03:00","forecast":110,"generation":105,"quarter1":90,"quarter2":95,"quarter3":120,"quarter4":125}]}""";
        var handler = new StubHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        });
        var client = new TransparencyApiClient(new TransparencyHttpClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") },
            new StubAuthentication()));

        var response = await client.GetWindGenerationForecastAsync(
            new DateRangeRequest(DateTimeOffset.Now, DateTimeOffset.Now), default);

        Assert.Single(response.Items);
        Assert.Equal(110m, response.Items[0].Forecast);
        Assert.Equal("/v1/renewables/data/res-generation-and-forecast", captured!.RequestUri!.AbsolutePath);
    }

    private static TransparencyAuthenticationClient CreateAuth(StubHandler handler) => new(new HttpClient(handler), Options.Create(new TransparencyOptions
    { BaseUrl = "https://example.test/", AuthenticationUrl = "https://auth.test/cas/v1/tickets", Username = "user", Password = "secret" }), TimeProvider.System);

    private sealed class StubAuthentication : ITransparencyAuthenticationClient { public Task<string> GetTicketAsync(bool forceRefresh, CancellationToken cancellationToken) => Task.FromResult("TGT-test"); }
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { CallCount++; return Task.FromResult(responder(request)); }
    }
}
