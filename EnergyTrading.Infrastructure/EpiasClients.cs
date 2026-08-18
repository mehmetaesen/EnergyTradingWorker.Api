using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnergyTrading.Application;
using Microsoft.Extensions.Options;

namespace EnergyTrading.Infrastructure;

public sealed class TransparencyAuthenticationException(string message) : Exception(message);
public sealed class TransparencyApiException(
    string message,
    HttpStatusCode statusCode,
    string? responseBody = null) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? ResponseBody { get; } = responseBody;
}
public sealed class TransparencyResponseDeserializationException(
    string message,
    HttpStatusCode statusCode,
    string responseBody,
    Exception? innerException = null) : Exception(message, innerException)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string ResponseBody { get; } = responseBody;
}
public sealed class TransparencyAuthenticationClient(HttpClient httpClient, IOptions<TransparencyOptions> options, TimeProvider timeProvider) : ITransparencyAuthenticationClient
{
    private readonly SemaphoreSlim _gate = new(1, 1); private string? _ticket; private DateTimeOffset _expiresAt;
    public async Task<string> GetTicketAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && IsValid()) return _ticket!; await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && IsValid()) return _ticket!; var settings = options.Value;
            if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password)) throw new TransparencyAuthenticationException("Transparency Platform credentials are not configured.");
            using var request = new HttpRequestMessage(HttpMethod.Post, settings.AuthenticationUrl) { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["username"] = settings.Username, ["password"] = settings.Password }) };
            request.Headers.Accept.ParseAdd("text/plain"); using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Created) throw new TransparencyAuthenticationException($"Transparency Platform authentication failed with HTTP {(int)response.StatusCode}.");
            var value = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim(); if (!value.StartsWith("TGT-", StringComparison.Ordinal)) throw new TransparencyAuthenticationException("Transparency Platform returned an invalid authentication ticket.");
            _ticket = value; _expiresAt = timeProvider.GetUtcNow().AddMinutes(settings.TicketLifetimeMinutes); return value;
        }
        finally { _gate.Release(); }
    }
    private bool IsValid() => _ticket is not null && timeProvider.GetUtcNow() < _expiresAt;
}
public sealed class TransparencyHttpClient(HttpClient httpClient, ITransparencyAuthenticationClient authenticationClient) : ITransparencyHttpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public async Task<TResponse> PostAsync<TRequest, TResponse>(string relativeUrl, TRequest request, CancellationToken cancellationToken)
    {
        var response = await SendAsync(relativeUrl, request, false, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            response.Dispose();
            response = await SendAsync(relativeUrl, request, true, cancellationToken);
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var safeDetail = BuildSafeErrorDetail(errorBody);
                throw new TransparencyApiException(
                    $"Transparency Platform request failed with HTTP {(int)response.StatusCode}.{safeDetail}",
                    response.StatusCode,
                    errorBody);
            }
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                return JsonSerializer.Deserialize<TResponse>(responseBody, JsonOptions)
                    ?? throw new TransparencyResponseDeserializationException(
                        "EPİAŞ response was null or empty.",
                        response.StatusCode,
                        responseBody);
            }
            catch (JsonException exception)
            {   
                throw new TransparencyResponseDeserializationException(
                    "EPİAŞ response could not be deserialized.",
                    response.StatusCode,
                    responseBody,
                    exception);
            }
        }
    }
    private async Task<HttpResponseMessage> SendAsync<TRequest>(string relativeUrl, TRequest body, bool refresh, CancellationToken ct)
    {
        var ticket = await authenticationClient.GetTicketAsync(refresh, ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl);
        request.Headers.TryAddWithoutValidation("TGT", ticket);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        return response;
    }

    private static string BuildSafeErrorDetail(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var parts = new List<string>();
            if (root.TryGetProperty("correlationId", out var correlationId)) parts.Add($"CorrelationId: {correlationId.GetString()}");
            if (root.TryGetProperty("spanIds", out var spanIds)) parts.Add($"SpanIds: {spanIds.GetString()}");
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                foreach (var error in errors.EnumerateArray())
                {
                    var code = error.TryGetProperty("errorCode", out var errorCode) ? errorCode.GetString() : null;
                    var message = error.TryGetProperty("errorMessage", out var errorMessage) ? errorMessage.GetString() : null;
                    parts.Add($"{code}: {message}".Trim(' ', ':'));
                }
            }
            return parts.Count == 0 ? string.Empty : $" Detail: {string.Join(" | ", parts)}";
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}

public sealed class TransparencyApiClient(ITransparencyHttpClient httpClient) : ITransparencyApiClient
{
    private const string McpPath = "v1/markets/dam/data/mcp";
    private const string SystemMarginalPricePath = "v1/markets/bpm/data/system-marginal-price";
    private const string LoadEstimationPlanPath = "v1/consumption/data/load-estimation-plan";
    private const string RealTimeConsumptionPath = "v1/consumption/data/realtime-consumption";
    private const string GenerationPlanPath = "v1/generation/data/dpp";
    private const string FirstVersionGenerationPlanPath = "v1/generation/data/dpp-first-version";
    private const string InjectionQuantityPath = "v1/generation/data/injection-quantity";
    private const string PrimaryFrequencyPricePath = "v1/markets/ancillary-services/data/primary-frequency-capacity-price";
    private const string SecondaryFrequencyPricePath = "v1/markets/ancillary-services/data/secondary-frequency-capacity-price";
    private const string SystemDirectionPath = "v1/markets/bpm/data/system-direction";
    private const string WindGenerationForecastPath = "v1/renewables/data/res-generation-and-forecast";

    public Task<McpResponse> GetMcpAsync(McpRequest request, CancellationToken cancellationToken) =>
        httpClient.PostAsync<McpRequest, McpResponse>(McpPath, request, cancellationToken);

    public Task<SystemMarginalPriceResponse> GetSystemMarginalPriceAsync(
        SystemMarginalPriceRequest request,
        CancellationToken cancellationToken) =>
        httpClient.PostAsync<SystemMarginalPriceRequest, SystemMarginalPriceResponse>(
            SystemMarginalPricePath,
            request,
            cancellationToken);

    public Task<LoadEstimationPlanResponse> GetLoadEstimationPlanAsync(DateRangeRequest request, CancellationToken cancellationToken) =>
        httpClient.PostAsync<DateRangeRequest, LoadEstimationPlanResponse>(LoadEstimationPlanPath, request, cancellationToken);

    public Task<RealTimeConsumptionResponse> GetRealTimeConsumptionAsync(DateRangeRequest request, CancellationToken cancellationToken) =>
        httpClient.PostAsync<DateRangeRequest, RealTimeConsumptionResponse>(RealTimeConsumptionPath, request, cancellationToken);

    public Task<GenerationPlanResponse> GetGenerationPlanAsync(GenerationPlanRequest request, bool firstVersion, CancellationToken cancellationToken) =>
        httpClient.PostAsync<GenerationPlanRequest, GenerationPlanResponse>(firstVersion ? FirstVersionGenerationPlanPath : GenerationPlanPath, request, cancellationToken);

    public Task<InjectionQuantityResponse> GetInjectionQuantityAsync(DateRangeRequest request, CancellationToken cancellationToken) =>
        httpClient.PostAsync<DateRangeRequest, InjectionQuantityResponse>(InjectionQuantityPath, request, cancellationToken);

    public Task<FrequencyCapacityPriceResponse> GetPrimaryFrequencyCapacityPriceAsync(DateRangeRequest request, CancellationToken cancellationToken) =>
        httpClient.PostAsync<DateRangeRequest, FrequencyCapacityPriceResponse>(PrimaryFrequencyPricePath, request, cancellationToken);

    public Task<FrequencyCapacityPriceResponse> GetSecondaryFrequencyCapacityPriceAsync(DateRangeRequest request, CancellationToken cancellationToken) =>
        httpClient.PostAsync<DateRangeRequest, FrequencyCapacityPriceResponse>(SecondaryFrequencyPricePath, request, cancellationToken);

    public Task<SystemDirectionResponse> GetSystemDirectionAsync(SystemDirectionRequest request, CancellationToken cancellationToken) =>
        httpClient.PostAsync<SystemDirectionRequest, SystemDirectionResponse>(SystemDirectionPath, request, cancellationToken);

    public Task<WindGenerationForecastResponse> GetWindGenerationForecastAsync(DateRangeRequest request, CancellationToken cancellationToken) =>
        httpClient.PostAsync<DateRangeRequest, WindGenerationForecastResponse>(WindGenerationForecastPath, request, cancellationToken);

    public Task<JsonElement> GetRawDataAsync(string path, object request, CancellationToken cancellationToken) =>
        httpClient.PostAsync<object, JsonElement>(path, request, cancellationToken);

    public Task<TResponse> GetDataAsync<TResponse>(string path, object request, CancellationToken cancellationToken) =>
        httpClient.PostAsync<object, TResponse>(path, request, cancellationToken);
}
