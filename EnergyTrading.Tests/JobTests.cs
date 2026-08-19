using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Tests;

public sealed class JobTests
{
    [Fact]
    public async Task New_records_are_inserted_and_counts_are_logged()
    {
        var fixture = new Fixture([]); await fixture.Job.ExecuteAsync(new JobExecutionContext("42", 0));
        Assert.Single(fixture.Repository.Inserted); Assert.Equal((1, 0, 1), fixture.Log.Success);
    }

    [Fact]
    public async Task Changed_records_are_updated_but_unchanged_records_are_not()
    {
        var existing = new MarketClearingPrice { Id = 9, Date = Fixture.Day, TimeOfPeriodId = 1, Price = 50, PriceUsd = 2, PriceEur = 1 };
        var changed = new Fixture([existing]); await changed.Job.ExecuteAsync(); Assert.Single(changed.Repository.Updated);
        existing.Price = 100; existing.PriceUsd = 3; existing.PriceEur = 2;
        var unchanged = new Fixture([existing]); await unchanged.Job.ExecuteAsync(); Assert.Empty(unchanged.Repository.Updated);
    }

    [Fact]
    public async Task Duplicate_API_items_do_not_create_duplicate_rows()
    {
        var fixture = new Fixture([], duplicate: true); await fixture.Job.ExecuteAsync(); Assert.Single(fixture.Repository.Inserted);
    }

    [Fact]
    public async Task Decimal_precision_beyond_database_scale_does_not_trigger_an_update()
    {
        var existing = new MarketClearingPrice
        {
            Date = Fixture.Day,
            TimeOfPeriodId = 1,
            Price = 100.123456m,
            PriceUsd = 3.123457m,
            PriceEur = 2.123456m,
        };
        var repository = new FakeRepository([existing]);
        var apiItem = new McpResponseItem(
            new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.FromHours(3)),
            "00:00-01:00",
            100.1234564m,
            3.1234565m,
            2.1234564m);
        var job = new MarketClearingPriceJob(
            new FakeLog(), new FakeClient([apiItem], false), repository, new FakeUnitOfWork(), new FakeClock());

        await job.ExecuteAsync();

        Assert.Empty(repository.Updated);
    }

    [Fact]
    public async Task API_error_marks_log_failed_and_is_rethrown()
    {
        var fixture = new Fixture([], fail: true); await Assert.ThrowsAsync<HttpRequestException>(() => fixture.Job.ExecuteAsync(new JobExecutionContext("9", 2)));
        Assert.True(fixture.Log.Failed); Assert.Equal(2, fixture.Log.RetryCount);
    }

    [Fact]
    public async Task Manual_date_range_cannot_exceed_31_days()
    {
        var fixture = new Fixture([]);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            fixture.Job.ExecuteAsync(Fixture.Day, Fixture.Day.AddDays(31)));
    }

    [Fact]
    public async Task Manual_date_range_accepts_exactly_31_days()
    {
        var fixture = new Fixture([]);

        await fixture.Job.ExecuteAsync(Fixture.Day, Fixture.Day.AddDays(30));

        Assert.Single(fixture.Repository.Inserted);
    }

    private sealed class Fixture
    {
        public static readonly DateOnly Day = new(2026, 8, 17);
        public FakeRepository Repository { get; }
        public FakeLog Log { get; } = new();
        public MarketClearingPriceJob Job { get; }
        public Fixture(List<MarketClearingPrice> existing, bool duplicate = false, bool fail = false)
        {
            Repository = new(existing); var items = new List<McpResponseItem> { new(new DateTimeOffset(2026,8,17,0,0,0,TimeSpan.FromHours(3)), "00:00-01:00", 100, 3, 2) };
            if (duplicate) items.Add(items[0]); Job = new(Log, new FakeClient(items, fail), Repository, new FakeUnitOfWork(), new FakeClock());
        }
    }
    private sealed class FakeClock : ITurkeyClock
    {
        public DateTimeOffset Now => new(2026, 8, 16, 12, 0, 0, TimeSpan.FromHours(3));
        public DateOnly Today => Fixture.Day.AddDays(-1);
    }
    private sealed class FakeClient(IReadOnlyList<McpResponseItem> items, bool fail) : ITransparencyApiClient
    {
        public Task<McpResponse> GetMcpAsync(McpRequest request, CancellationToken ct) => fail ? throw new HttpRequestException("remote failed") : Task.FromResult(new McpResponse(items));
        public Task<SystemMarginalPriceResponse> GetSystemMarginalPriceAsync(SystemMarginalPriceRequest request, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class FakeUnitOfWork : IUnitOfWork { public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) => action(ct); }
    private sealed class FakeRepository(List<MarketClearingPrice> existing) : IGenericRepository<MarketClearingPrice>
    {
        public List<MarketClearingPrice> Inserted { get; } = []; public List<MarketClearingPrice> Updated { get; } = [];
        public Task<List<MarketClearingPrice>> GetListAsync(DateOnly date, IReadOnlyCollection<int> ids, CancellationToken ct) => Task.FromResult(existing.Where(x => x.Date == date && ids.Contains(x.TimeOfPeriodId)).ToList());
        public Task<List<MarketClearingPrice>> GetDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct) => Task.FromResult(existing.Where(x => x.Date >= start && x.Date <= end).ToList());
        public Task InsertAsync(IReadOnlyCollection<MarketClearingPrice> entities, CancellationToken ct) { Inserted.AddRange(entities); return Task.CompletedTask; }
        public Task UpdateAsync(IReadOnlyCollection<MarketClearingPrice> entities, CancellationToken ct) { Updated.AddRange(entities); return Task.CompletedTask; }
    }
    private sealed class FakeLog : IIntegrationJobLogService
    {
        public (int Inserted, int Updated, int Fetched) Success { get; private set; } public bool Failed { get; private set; } public int RetryCount { get; private set; }
        public Task<(long JobId, long LogId)> StartAsync(string code, JobExecutionContext context, Guid correlationId, CancellationToken ct) { RetryCount = context.RetryCount; return Task.FromResult((1L, 2L)); }
        public Task CompleteAsync(long id, int fetched, SaveResult result, CancellationToken ct) { Success = (result.InsertedCount, result.UpdatedCount, fetched); return Task.CompletedTask; }
        public Task FailAsync(long id, Exception exception, CancellationToken ct) { Failed = true; return Task.CompletedTask; }
    }
}
