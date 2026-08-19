using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Tests;

public sealed class AdditionalTransparencyJobTests
{
    [Fact]
    public async Task Wind_generation_job_maps_hour_and_quarter_values()
    {
        var repository = new FakeRepository<WindGenerationAndForecast>();
        var job = new WindGenerationForecastJob(
            new FakeLog(), new FakeClient(), repository, new FakeUnitOfWork(), new FakeClock());

        await job.ExecuteAsync();

        var entity = Assert.Single(repository.Inserted);
        Assert.Equal(2, entity.TimeOfPeriodId);
        Assert.Equal(2, entity.Quarter);
        Assert.Equal(new TimeOnly(1, 15), entity.Hour);
        Assert.Equal(110m, entity.Forecast);
        Assert.Equal(105m, entity.Generation);
        Assert.Equal(90m, entity.Quantile5);
        Assert.Equal(125m, entity.Quantile95);
    }

    [Fact]
    public async Task Real_time_generation_job_maps_documented_response_fields()
    {
        var repository = new FakeRepository<RealTimeGeneration>();
        var client = new TypedFakeClient();
        var job = new RealTimeGenerationJob(
            new FakeLog(), client, repository, new FakeUnitOfWork(), new FakeClock());

        await job.ExecuteAsync();

        var entity = Assert.Single(repository.Inserted);
        Assert.Equal(new DateOnly(2026, 8, 17), entity.Date);
        Assert.Equal(1, entity.TimeOfPeriodId);
        Assert.Equal(42.5m, entity.NaturalGas);
        Assert.Equal(100m, entity.Total);
        Assert.Equal("v1/generation/data/realtime-generation", client.Path);
    }

    [Fact]
    public async Task Real_time_consumption_uses_string_time_and_limits_end_to_two_hours_ago()
    {
        var repository = new FakeRepository<RealTimeConsumption>();
        var client = new RealTimeConsumptionFakeClient();
        var job = new RealTimeConsumptionJob(
            new FakeLog(), client, repository, new FakeUnitOfWork(), new FakeClock());

        await job.ExecuteAsync(new DateOnly(2026, 8, 18), new DateOnly(2026, 8, 18));

        var entity = Assert.Single(repository.Inserted);
        Assert.Equal(9, entity.TimeOfPeriodId);
        Assert.Equal(123.45m, entity.Consumption);
        Assert.Equal(new DateTimeOffset(2026, 8, 18, 10, 0, 0, TimeSpan.FromHours(3)), client.Request!.EndDate);
    }

    [Fact]
    public async Task Withdrawal_quantity_converts_period_to_utc_before_saving()
    {
        var repository = new FakeRepository<WithdrawalQuantity>();
        var job = new WithdrawalQuantityJob(
            new FakeLog(), new WithdrawalQuantityFakeClient(), repository,
            new FakeUnitOfWork(), new FakeClock(), new FakeRegionProvider());

        await job.ExecuteAsync(new DateOnly(2026, 8, 17), new DateOnly(2026, 8, 17));

        var entity = Assert.Single(repository.Inserted);
        Assert.Equal(TimeSpan.Zero, entity.Period.Offset);
        Assert.Equal(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero), entity.Period);
        Assert.Equal(12.5m, entity.Swv);
    }

    [Fact]
    public async Task Unlicensed_generation_accepts_iso_datetime_in_time_field()
    {
        var repository = new FakeRepository<UnlicensedGenerationAmount>();
        var job = new UnlicensedGenerationJob(
            new FakeLog(), new UnlicensedGenerationFakeClient(), repository,
            new FakeUnitOfWork(), new FakeClock(), new FakeRegionProvider());

        await job.ExecuteAsync(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 1));

        var entity = Assert.Single(repository.Inserted);
        Assert.Equal(new DateOnly(2020, 1, 1), entity.Date);
        Assert.Equal(1, entity.TimeOfPeriodId);
        Assert.Equal(28m, entity.Total);
    }

    private sealed class FakeClient : ITransparencyApiClient
    {
        public Task<McpResponse> GetMcpAsync(McpRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<SystemMarginalPriceResponse> GetSystemMarginalPriceAsync(SystemMarginalPriceRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<WindGenerationForecastResponse> GetWindGenerationForecastAsync(DateRangeRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new WindGenerationForecastResponse([
                new(new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.FromHours(3)),
                    new DateTimeOffset(2026, 8, 18, 1, 15, 0, TimeSpan.FromHours(3)),
                    110, 105, 90, 95, 120, 125)
            ]));
    }

    private sealed class FakeClock : ITurkeyClock
    {
        public DateTimeOffset Now => new(2026, 8, 18, 12, 0, 0, TimeSpan.FromHours(3));
        public DateOnly Today => new(2026, 8, 18);
    }

    private sealed class FakeRegionProvider : ITransparencyRegionProvider { public string SystemMarginalPriceRegion => "TR1"; }

    private sealed class TypedFakeClient : ITransparencyApiClient
    {
        public string? Path { get; private set; }
        public Task<McpResponse> GetMcpAsync(McpRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<SystemMarginalPriceResponse> GetSystemMarginalPriceAsync(SystemMarginalPriceRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TResponse> GetDataAsync<TResponse>(string path, object request, CancellationToken cancellationToken)
        {
            Path = path;
            object response = new RealTimeGenerationResponse([
                new(new DateTimeOffset(2026,8,17,0,0,0,TimeSpan.FromHours(3)), "00:00-01:00",
                    1,2,3,4,5,6,7,8,9,10,11,42.5m,13,14,100,15,16)
            ]);
            return Task.FromResult((TResponse)response);
        }
    }

    private sealed class RealTimeConsumptionFakeClient : ITransparencyApiClient
    {
        public DateRangeRequest? Request { get; private set; }

        public Task<McpResponse> GetMcpAsync(McpRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<SystemMarginalPriceResponse> GetSystemMarginalPriceAsync(
            SystemMarginalPriceRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RealTimeConsumptionResponse> GetRealTimeConsumptionAsync(
            DateRangeRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new RealTimeConsumptionResponse([
                new(new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.FromHours(3)), "08:00", 123.45m)
            ]));
        }
    }

    private sealed class WithdrawalQuantityFakeClient : ITransparencyApiClient
    {
        public Task<McpResponse> GetMcpAsync(McpRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<SystemMarginalPriceResponse> GetSystemMarginalPriceAsync(
            SystemMarginalPriceRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TResponse> GetDataAsync<TResponse>(
            string path,
            object request,
            CancellationToken cancellationToken)
        {
            object response = new WithdrawalQuantityResponse([
                new(
                    new DateTimeOffset(2026, 8, 17, 3, 0, 0, TimeSpan.FromHours(3)),
                    new DateTimeOffset(2026, 8, 17, 3, 0, 0, TimeSpan.FromHours(3)),
                    12.5m)
            ]);
            return Task.FromResult((TResponse)response);
        }
    }

    private sealed class UnlicensedGenerationFakeClient : ITransparencyApiClient
    {
        public Task<McpResponse> GetMcpAsync(McpRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<SystemMarginalPriceResponse> GetSystemMarginalPriceAsync(
            SystemMarginalPriceRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TResponse> GetDataAsync<TResponse>(
            string path,
            object request,
            CancellationToken cancellationToken)
        {
            object response = new UnlicensedGenerationResponse([
                new(
                    new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(3)),
                    "2020-01-01T00:00:00+03:00",
                    1m, 2m, 3m, 4m, 5m, 6m, 28m)
            ]);
            return Task.FromResult((TResponse)response);
        }
    }

    private sealed class FakeRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity, IPeriodEntity
    {
        public List<TEntity> Inserted { get; } = [];
        public Task<List<TEntity>> GetListAsync(DateOnly date, IReadOnlyCollection<int> ids, CancellationToken ct) => Task.FromResult(new List<TEntity>());
        public Task<List<TEntity>> GetDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct) => Task.FromResult(new List<TEntity>());
        public Task InsertAsync(IReadOnlyCollection<TEntity> entities, CancellationToken ct) { Inserted.AddRange(entities); return Task.CompletedTask; }
        public Task UpdateAsync(IReadOnlyCollection<TEntity> entities, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) => action(cancellationToken);
    }

    private sealed class FakeLog : IIntegrationJobLogService
    {
        public Task<(long JobId, long LogId)> StartAsync(string jobCode, JobExecutionContext context, Guid correlationId, CancellationToken cancellationToken) => Task.FromResult((4L, 1L));
        public Task CompleteAsync(long logId, int fetchedCount, SaveResult result, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task FailAsync(long logId, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
