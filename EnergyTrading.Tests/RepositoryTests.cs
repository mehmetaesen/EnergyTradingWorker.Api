using EnergyTrading.Domain;
using EnergyTrading.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EnergyTrading.Tests;
public sealed class RepositoryTests
{
    [Fact]
    public async Task GetList_filters_date_and_period_and_empty_periods_return_nothing()
    {
        var options = new DbContextOptionsBuilder<EnergyTradingDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new EnergyTradingDbContext(options); var day = new DateOnly(2026, 8, 17);
        db.MarketClearingPrices.AddRange(new() { Date = day, TimeOfPeriodId = 1 }, new() { Date = day, TimeOfPeriodId = 2 }, new() { Date = day.AddDays(1), TimeOfPeriodId = 1 }); await db.SaveChangesAsync();
        var repository = new EfGenericRepository<MarketClearingPrice>(db);
        var result = await repository.GetListAsync(day, [2], default); Assert.Single(result); Assert.Equal(2, result[0].TimeOfPeriodId);
        Assert.Empty(await repository.GetListAsync(day, [], default));
    }

    [Fact]
    public async Task Deserialization_failure_response_is_saved_to_job_log()
    {
        var options = new DbContextOptionsBuilder<EnergyTradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var db = new EnergyTradingDbContext(options))
        {
            db.IntegrationJobs.Add(new IntegrationJob { Id = 1, Code = "TEST" });
            db.IntegrationJobLogs.Add(new IntegrationJobLog
            {
                Id = 2,
                IntegrationJobId = 1,
                CorrelationId = Guid.NewGuid(),
                Status = IntegrationJobStatus.Running,
                StartedDate = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        const string responseBody = "\"Şeffaflık servisi geçici olarak kullanılamıyor\"";
        var service = new IntegrationJobLogService(new TestDbContextFactory(options));
        await service.FailAsync(
            2,
            new TransparencyResponseDeserializationException(
                "EPİAŞ response could not be deserialized.",
                System.Net.HttpStatusCode.OK,
                responseBody),
            default);

        await using var verificationDb = new EnergyTradingDbContext(options);
        var log = await verificationDb.IntegrationJobLogs.SingleAsync(x => x.Id == 2);
        Assert.Equal(responseBody, log.ResponseBody);
        Assert.Equal(200, log.ResponseCode);
        Assert.Equal(IntegrationJobStatus.Failed, log.Status);
    }

    [Fact]
    public async Task Api_error_response_is_saved_to_job_log()
    {
        var options = new DbContextOptionsBuilder<EnergyTradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var db = new EnergyTradingDbContext(options))
        {
            db.IntegrationJobs.Add(new IntegrationJob { Id = 1, Code = "TEST" });
            db.IntegrationJobLogs.Add(new IntegrationJobLog
            {
                Id = 2,
                IntegrationJobId = 1,
                CorrelationId = Guid.NewGuid(),
                Status = IntegrationJobStatus.Running,
                StartedDate = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        const string responseBody = "upstream error";
        var service = new IntegrationJobLogService(new TestDbContextFactory(options));
        await service.FailAsync(
            2,
            new TransparencyApiException(
                "Transparency Platform request failed with HTTP 502.",
                System.Net.HttpStatusCode.BadGateway,
                responseBody),
            default);

        await using var verificationDb = new EnergyTradingDbContext(options);
        var log = await verificationDb.IntegrationJobLogs.SingleAsync(x => x.Id == 2);
        Assert.Equal(responseBody, log.ResponseBody);
        Assert.Equal(502, log.ResponseCode);
    }

    private sealed class TestDbContextFactory(DbContextOptions<EnergyTradingDbContext> options)
        : IDbContextFactory<EnergyTradingDbContext>
    {
        public EnergyTradingDbContext CreateDbContext() => new(options);
    }
}
