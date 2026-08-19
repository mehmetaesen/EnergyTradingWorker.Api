using EnergyTrading.Domain;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace EnergyTrading.Application;

public sealed record SaveResult(int InsertedCount, int UpdatedCount);
public sealed record ReconciliationResult(
    string JobCode,
    DateOnly StartDate,
    DateOnly EndDate,
    int ApiRecordCount,
    int ApiUniqueRecordCount,
    int DatabaseRecordCount,
    int MissingRecordCount,
    int DifferentRecordCount,
    int ExtraRecordCount,
    IReadOnlyList<string> MissingKeys,
    IReadOnlyList<string> DifferentKeys,
    IReadOnlyList<string> ExtraKeys)
{
    public bool IsMatch => MissingRecordCount == 0 && DifferentRecordCount == 0 && ExtraRecordCount == 0;
}

public interface ITransparencyReconciliationJob
{
    Task<ReconciliationResult> ReconcileAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);
}
public sealed record JobExecutionContext(string? HangfireJobId = null, int RetryCount = 0);

public interface IGenericRepository<TEntity> where TEntity : BaseEntity, IPeriodEntity
{
    Task<List<TEntity>> GetListAsync(DateOnly date, IReadOnlyCollection<int> timeOfPeriodIds, CancellationToken cancellationToken);
    Task<List<TEntity>> GetDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);
    Task InsertAsync(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken);
    Task UpdateAsync(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}

public interface IIntegrationJobLogService
{
    Task<(long JobId, long LogId)> StartAsync(string jobCode, JobExecutionContext context, Guid correlationId, CancellationToken cancellationToken);
    Task CompleteAsync(long logId, int fetchedCount, SaveResult result, CancellationToken cancellationToken);
    Task FailAsync(long logId, Exception exception, CancellationToken cancellationToken);
}


public sealed class TransparencyOptions
{
    public const string SectionName = "Transparency";
    public required string BaseUrl { get; init; }
    public required string AuthenticationUrl { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 60;
    public int TicketLifetimeMinutes { get; init; } = 115;
    public string SystemMarginalPriceRegion { get; init; } = "TR1";
}

public sealed record McpRequest(DateTimeOffset StartDate, DateTimeOffset EndDate, PageRequest? Page = null);
public sealed record PageRequest(int Number = 1, int Size = 100);
public sealed record McpResponse(IReadOnlyList<McpResponseItem> Items);
public sealed record McpResponseItem(DateTimeOffset Date, string Hour, decimal Price, decimal PriceUsd, decimal PriceEur);
public sealed record MarketClearingPriceDto(DateOnly Date, int TimeOfPeriodId, decimal Price, decimal PriceUsd, decimal PriceEur);
public sealed record SystemMarginalPriceRequest(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? Region = null,
    PageRequest? Page = null);
public sealed record SystemMarginalPriceResponse(IReadOnlyList<SystemMarginalPriceResponseItem> Items);
public sealed record SystemMarginalPriceResponseItem(
    DateTimeOffset Date,
    DateTimeOffset Hour,
    decimal SystemMarginalPrice);
public sealed record SystemMarginalPriceDto(DateOnly Date, int TimeOfPeriodId, decimal Price);

public interface ITransparencyAuthenticationClient
{
    Task<string> GetTicketAsync(bool forceRefresh, CancellationToken cancellationToken);
}

public interface ITransparencyApiClient
{
    Task<McpResponse> GetMcpAsync(McpRequest request, CancellationToken cancellationToken);
    Task<SystemMarginalPriceResponse> GetSystemMarginalPriceAsync(
        SystemMarginalPriceRequest request,
        CancellationToken cancellationToken);
    Task<LoadEstimationPlanResponse> GetLoadEstimationPlanAsync(DateRangeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<RealTimeConsumptionResponse> GetRealTimeConsumptionAsync(DateRangeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<GenerationPlanResponse> GetGenerationPlanAsync(GenerationPlanRequest request, bool firstVersion, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<InjectionQuantityResponse> GetInjectionQuantityAsync(DateRangeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<FrequencyCapacityPriceResponse> GetPrimaryFrequencyCapacityPriceAsync(DateRangeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<FrequencyCapacityPriceResponse> GetSecondaryFrequencyCapacityPriceAsync(DateRangeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<SystemDirectionResponse> GetSystemDirectionAsync(SystemDirectionRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<WindGenerationForecastResponse> GetWindGenerationForecastAsync(DateRangeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<TResponse> GetDataAsync<TResponse>(string path, object request, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<JsonElement> GetRawDataAsync(string path, object request, CancellationToken cancellationToken) => throw new NotSupportedException();
}

public sealed record DateRangeRequest(DateTimeOffset StartDate, DateTimeOffset EndDate, PageRequest? Page = null);
public sealed record GenerationPlanRequest(DateTimeOffset StartDate, DateTimeOffset EndDate, string Region = "TR1", long? OrganizationId = null, long? UevcbId = null, PageRequest? Page = null);
public sealed record SystemDirectionRequest(DateTimeOffset StartDate, DateTimeOffset EndDate, string Region = "TR1", PageRequest? Page = null);

public sealed record LoadEstimationPlanResponse(IReadOnlyList<LoadEstimationPlanItem> Items);
public sealed record LoadEstimationPlanItem(DateTimeOffset Date, string Time, decimal Lep);
public sealed record RealTimeConsumptionResponse(IReadOnlyList<RealTimeConsumptionItem> Items);
public sealed record RealTimeConsumptionItem(DateTimeOffset Date, string Time, decimal Consumption);

public sealed record GenerationPlanResponse(IReadOnlyList<GenerationPlanItem> Items);
public sealed record GenerationPlanItem(
    DateTimeOffset Date,
    string Time,
    [property: JsonPropertyName("akarsu")] decimal? River,
    [property: JsonPropertyName("barajli")] decimal? Dam,
    [property: JsonPropertyName("biokutle")] decimal? Biomass,
    [property: JsonPropertyName("diger")] decimal? Other,
    [property: JsonPropertyName("dogalgaz")] decimal? NaturalGas,
    decimal? FuelOil,
    [property: JsonPropertyName("gunes")] decimal? Solar,
    [property: JsonPropertyName("ithalKomur")] decimal? ImportedCoal,
    [property: JsonPropertyName("jeotermal")] decimal? Geothermal,
    [property: JsonPropertyName("linyit")] decimal? Lignite,
    [property: JsonPropertyName("nafta")] decimal? Naphtha,
    [property: JsonPropertyName("ruzgar")] decimal? Wind,
    [property: JsonPropertyName("tasKomur")] decimal? HardCoal,
    [property: JsonPropertyName("toplam")] decimal? Total);

public sealed record InjectionQuantityResponse(IReadOnlyList<InjectionQuantityItem> Items);
public sealed record InjectionQuantityItem(
    DateTimeOffset Date, int Hour, decimal Asphaltite, decimal Biomass, decimal Dam,
    decimal Fueloil, decimal Geothermal, decimal ImportedCoal, decimal InternationalExport,
    decimal InternationalImport, decimal Lignite, decimal Lng, decimal Naphtha,
    decimal NaturalGas, decimal Other, decimal River, decimal StoneCoal, decimal Sun,
    decimal Wind, decimal Total);

public sealed record FrequencyCapacityPriceResponse(IReadOnlyList<FrequencyCapacityPriceItem> Items);
public sealed record FrequencyCapacityPriceItem(DateTimeOffset Date, int Hour, decimal Price);
public sealed record SystemDirectionResponse(IReadOnlyList<SystemDirectionItem> Items);
public sealed record SystemDirectionItem(DateTimeOffset Date, string Hour, long? SmpDirectionId, string? SystemDirection);
public sealed record WindGenerationForecastResponse(IReadOnlyList<WindGenerationForecastItem> Items);
public sealed record WindGenerationForecastItem(
    DateTimeOffset Date, DateTimeOffset Time, decimal Forecast, decimal? Generation,
    decimal Quarter1, decimal Quarter2, decimal Quarter3, decimal Quarter4);

public sealed record InstalledCapacityResponse(IReadOnlyList<InstalledCapacityItem> InstalledCapacities);
public sealed record InstalledCapacityItem(decimal LicencedCapacity, string RenewableEnergyType, decimal Total, decimal UnlicencedCapacity);
public sealed record PowerOutageResponse(IReadOnlyList<PowerOutageItem> Items);
public sealed record PowerOutageItem(DateTimeOffset Date, string DistributionCompanyName, string District,
    string EffectedNeighbourhoods, long EffectedSubscribers, DateTimeOffset EndTime, decimal HourlyLoadAvg,
    long Id, string Province, string Reason, DateTimeOffset StartTime);
public sealed record UnlicensedGenerationResponse(IReadOnlyList<UnlicensedGenerationItem> Items);
public sealed record UnlicensedGenerationItem(DateTimeOffset Date, string Time, decimal Biyogaz, decimal Biokutle,
    decimal Diger, decimal Gunes, decimal KanalTipi, decimal Ruzgar, decimal Toplam);
public sealed record RealTimeGenerationResponse(IReadOnlyList<RealTimeGenerationItem> Items);
public sealed record RealTimeGenerationItem(DateTimeOffset Date, string Hour, decimal AsphaltiteCoal, decimal Biomass,
    decimal BlackCoal, decimal DammedHydro, decimal Fueloil, decimal Geothermal, decimal ImportCoal,
    decimal ImportExport, decimal Lignite, decimal Lng, decimal Naphta, decimal NaturalGas, decimal River,
    decimal Sun, decimal Total, decimal Wasteheat, decimal Wind);
public sealed record OrderSummaryUpResponse(IReadOnlyList<OrderSummaryUpItem> Items);
public sealed record OrderSummaryUpItem(DateTimeOffset Date, string Hour, decimal Net, decimal UpRegulationDelivered,
    decimal UpRegulationOneCoded, decimal UpRegulationTwoCoded, decimal UpRegulationZeroCoded);
public sealed record OrderSummaryDownResponse(IReadOnlyList<OrderSummaryDownItem> Items);
public sealed record OrderSummaryDownItem(DateTimeOffset Date, string Hour, decimal Net, decimal DownRegulationDelivered,
    decimal DownRegulationOneCoded, decimal DownRegulationTwoCoded, decimal DownRegulationZeroCoded);
public sealed record ClearingQuantityResponse(IReadOnlyList<ClearingQuantityItem> Items);
public sealed record ClearingQuantityItem(DateTimeOffset Date, string Hour, decimal MatchedBids, decimal MatchedOffers);
public sealed record WeightedAveragePriceResponse(IReadOnlyList<WeightedAveragePriceItem> Items);
public sealed record WeightedAveragePriceItem(DateTimeOffset Date, string Hour, decimal Wap);
public sealed record MatchingQuantityResponse(IReadOnlyList<MatchingQuantityItem> Items);
public sealed record MatchingQuantityItem(decimal ClearingQuantityAsk, decimal ClearingQuantityBid, string KontratAdi, string KontratTuru);
public sealed record WithdrawalQuantityResponse(IReadOnlyList<WithdrawalQuantityItem> Items);
public sealed record WithdrawalQuantityItem(DateTimeOffset Hour, DateTimeOffset Period, decimal Swv);

public interface ITransparencyRegionProvider
{
    string SystemMarginalPriceRegion { get; }
}

public interface ITransparencyHttpClient
{
    Task<TResponse> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest request,
        CancellationToken cancellationToken);
}

public interface ITurkeyClock
{
    DateTimeOffset Now { get; }
    DateOnly Today { get; }
}

public sealed class TurkeyClock(TimeProvider timeProvider) : ITurkeyClock
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Zone);
    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
}
