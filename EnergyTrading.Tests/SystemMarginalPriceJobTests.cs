using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Tests;

public sealed class SystemMarginalPriceJobTests
{
    private static readonly DateOnly Day = new(2026, 8, 17);

    [Fact]
    public async Task Fetches_the_complete_current_day_and_inserts_missing_periods()
    {
        var client = new FakeClient();
        var repository = new FakeRepository([]);
        var log = new FakeLog();
        var job = new SystemMarginalPriceJob(log, client, repository, new FakeUnitOfWork(), new FakeClock(), new FakeRegionProvider());

        await job.ExecuteAsync();

        Assert.Equal(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.FromHours(3)), client.Request!.StartDate);
        Assert.Equal(new DateTimeOffset(2026, 8, 17, 20, 59, 0, TimeSpan.FromHours(3)), client.Request.EndDate);
        Assert.Single(repository.Inserted);
        Assert.Equal(1, log.Result!.InsertedCount);
    }

    [Fact]
    public async Task Updates_changed_price_and_skips_unchanged_price()
    {
        var changed = new SystemMarginalPrice { Date = Day, TimeOfPeriodId = 1, Price = 50 };
        var changedRepository = new FakeRepository([changed]);
        await new SystemMarginalPriceJob(new FakeLog(), new FakeClient(), changedRepository, new FakeUnitOfWork(), new FakeClock(), new FakeRegionProvider()).ExecuteAsync();
        Assert.Single(changedRepository.Updated);

        var unchanged = new SystemMarginalPrice { Date = Day, TimeOfPeriodId = 1, Price = 100 };
        var unchangedRepository = new FakeRepository([unchanged]);
        await new SystemMarginalPriceJob(new FakeLog(), new FakeClient(), unchangedRepository, new FakeUnitOfWork(), new FakeClock(), new FakeRegionProvider()).ExecuteAsync();
        Assert.Empty(unchangedRepository.Updated);
    }

    private sealed class FakeClock : ITurkeyClock
    {
        public DateTimeOffset Now => new(2026, 8, 17, 21, 0, 0, TimeSpan.FromHours(3));
        public DateOnly Today => Day;
    }
    private sealed class FakeRegionProvider : ITransparencyRegionProvider { public string SystemMarginalPriceRegion => "TR1"; }
    private sealed class FakeUnitOfWork : IUnitOfWork { public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) => action(ct); }
    private sealed class FakeClient : ITransparencyApiClient
    {
        public SystemMarginalPriceRequest? Request { get; private set; }
        public Task<McpResponse> GetMcpAsync(McpRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<SystemMarginalPriceResponse> GetSystemMarginalPriceAsync(SystemMarginalPriceRequest request, CancellationToken ct)
        {
            Request = request;
            return Task.FromResult(new SystemMarginalPriceResponse([
                new(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.FromHours(3)), new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.FromHours(3)), 100)
            ]));
        }
    }
    private sealed class FakeRepository(List<SystemMarginalPrice> existing) : IGenericRepository<SystemMarginalPrice>
    {
        public List<SystemMarginalPrice> Inserted { get; } = [];
        public List<SystemMarginalPrice> Updated { get; } = [];
        public Task<List<SystemMarginalPrice>> GetListAsync(DateOnly date, IReadOnlyCollection<int> ids, CancellationToken ct) => Task.FromResult(existing.Where(x => x.Date == date && ids.Contains(x.TimeOfPeriodId)).ToList());
        public Task<List<SystemMarginalPrice>> GetDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct) => Task.FromResult(existing.Where(x => x.Date >= start && x.Date <= end).ToList());
        public Task InsertAsync(IReadOnlyCollection<SystemMarginalPrice> entities, CancellationToken ct) { Inserted.AddRange(entities); return Task.CompletedTask; }
        public Task UpdateAsync(IReadOnlyCollection<SystemMarginalPrice> entities, CancellationToken ct) { Updated.AddRange(entities); return Task.CompletedTask; }
    }
    private sealed class FakeLog : IIntegrationJobLogService
    {
        public SaveResult? Result { get; private set; }
        public Task<(long JobId, long LogId)> StartAsync(string code, JobExecutionContext context, Guid correlationId, CancellationToken ct) => Task.FromResult((2L, 2L));
        public Task CompleteAsync(long id, int fetched, SaveResult result, CancellationToken ct) { Result = result; return Task.CompletedTask; }
        public Task FailAsync(long id, Exception exception, CancellationToken ct) => Task.CompletedTask;
    }
}
