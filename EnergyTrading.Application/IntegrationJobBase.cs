namespace EnergyTrading.Application;

public abstract class IntegrationJobBase<TFetchedData>(IIntegrationJobLogService logService)
{
    protected abstract string JobCode { get; }
    protected abstract (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange();
    protected abstract Task<IReadOnlyList<TFetchedData>> FetchAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);
    protected virtual Task SendAsync(IReadOnlyList<TFetchedData> data, CancellationToken cancellationToken) => Task.CompletedTask;
    protected abstract Task<SaveResult> SaveAsync(IReadOnlyList<TFetchedData> data, CancellationToken cancellationToken);

    public async Task ExecuteAsync(JobExecutionContext? context = null, CancellationToken cancellationToken = default)
    {
        var range = GetDefaultDateRange();
        await ExecuteAsync(range.StartDate, range.EndDate, context, cancellationToken);
    }

    public async Task ExecuteAsync(
        DateOnly startDate,
        DateOnly endDate,
        JobExecutionContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(startDate, endDate);
        context ??= new JobExecutionContext();
        var correlationId = Guid.NewGuid();
        var (_, logId) = await logService.StartAsync(JobCode, context, correlationId, cancellationToken);
        try
        {
            var data = await FetchAsync(startDate, endDate, cancellationToken);
            await SendAsync(data, cancellationToken);
            var result = await SaveAsync(data, cancellationToken);
            await logService.CompleteAsync(logId, data.Count, result, cancellationToken);
        }
        catch (Exception exception)
        {
            await logService.FailAsync(logId, exception, CancellationToken.None);
            throw;
        }
    }

    private static void ValidateDateRange(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot be earlier than start date.");
        if (endDate > startDate.AddMonths(1))
            throw new ArgumentOutOfRangeException(nameof(endDate), "The selected date range cannot exceed one calendar month.");
    }
}
