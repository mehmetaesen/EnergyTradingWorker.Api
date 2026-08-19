using System.Diagnostics;
using EnergyTrading.Application;
using EnergyTrading.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnergyTrading.Infrastructure;

public sealed class EfGenericRepository<TEntity>(EnergyTradingDbContext db) : IGenericRepository<TEntity> where TEntity : BaseEntity, IPeriodEntity
{
    public Task<List<TEntity>> GetListAsync(DateOnly date, IReadOnlyCollection<int> timeOfPeriodIds, CancellationToken cancellationToken)
    {
        if (timeOfPeriodIds.Count == 0) return Task.FromResult(new List<TEntity>());
        return db.Set<TEntity>().AsNoTracking().Where(x => x.Date == date && timeOfPeriodIds.Contains(x.TimeOfPeriodId)).ToListAsync(cancellationToken);
    }
    public Task<List<TEntity>> GetDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
        db.Set<TEntity>().AsNoTracking().Where(x => x.Date >= startDate && x.Date <= endDate).ToListAsync(cancellationToken);
    public async Task InsertAsync(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken)
    {
        if (entities.Count == 0) return; await db.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
    }
    public Task UpdateAsync(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken)
    {
        if (entities.Count > 0) db.Set<TEntity>().UpdateRange(entities); return Task.CompletedTask;
    }
}

public sealed class EfUnitOfWork(EnergyTradingDbContext db) : IUnitOfWork
{
    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var result = await action(cancellationToken); await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return result;
    }
}

public sealed class IntegrationJobLogService(IDbContextFactory<EnergyTradingDbContext> factory) : IIntegrationJobLogService
{
    public async Task<(long JobId, long LogId)> StartAsync(string jobCode, JobExecutionContext context, Guid correlationId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var job = await db.IntegrationJobs.SingleOrDefaultAsync(x => x.Code == jobCode && x.IsActive, ct)
            ?? throw new InvalidOperationException($"Active integration job '{jobCode}' was not found.");
        var log = new IntegrationJobLog { IntegrationJobId = job.Id, HangfireJobId = context.HangfireJobId, CorrelationId = correlationId,
            RetryCount = context.RetryCount, Status = IntegrationJobStatus.Running, StartedDate = DateTimeOffset.UtcNow };
        db.IntegrationJobLogs.Add(log); await db.SaveChangesAsync(ct); return (job.Id, log.Id);
    }
    public async Task CompleteAsync(long logId, int fetchedCount, SaveResult result, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var log = await db.IntegrationJobLogs.SingleAsync(x => x.Id == logId, ct);
        log.Status = IntegrationJobStatus.Succeeded; log.IsSuccess = true; log.FetchedRecordCount = fetchedCount;
        log.InsertedRecordCount = result.InsertedCount; log.UpdatedRecordCount = result.UpdatedCount; Finish(log); await db.SaveChangesAsync(ct);
    }
    public async Task FailAsync(long logId, Exception exception, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var log = await db.IntegrationJobLogs.SingleAsync(x => x.Id == logId, ct);
        log.Status = IntegrationJobStatus.Failed; log.IsSuccess = false; log.ErrorDescription = Sanitize(exception.Message);
        if (exception is TransparencyResponseDeserializationException deserializationException)
        {
            log.ResponseCode = (int)deserializationException.StatusCode;
            log.ResponseBody = deserializationException.ResponseBody;
        }
        else if (exception is TransparencyApiException apiException)
        {
            log.ResponseCode = (int)apiException.StatusCode;
            log.ResponseBody = apiException.ResponseBody;
        }
        Finish(log); await db.SaveChangesAsync(ct);
    }
    private static void Finish(IntegrationJobLog log) { log.CompletedDate = DateTimeOffset.UtcNow; log.DurationMilliseconds = (long)(log.CompletedDate.Value - log.StartedDate).TotalMilliseconds; }
    private static string Sanitize(string message) => message.Length <= 4000 ? message : message[..4000];
}
