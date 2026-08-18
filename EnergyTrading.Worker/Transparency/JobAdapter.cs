using EnergyTrading.Application;
using Hangfire.Server;

namespace EnergyTrading.Worker.Transparency;

public abstract class JobAdapter<TData>(IntegrationJobBase<TData> job)
{
    protected Task ExecuteCoreAsync(
        DateOnly? start,
        DateOnly? end,
        PerformContext? context,
        CancellationToken ct
    )
    {
        var execution = new JobExecutionContext(
            context?.BackgroundJob?.Id,
            context?.GetJobParameter<int>("RetryCount") ?? 0
        );
        if (start is null && end is null)
            return job.ExecuteAsync(execution, ct);
        if (start is null || end is null)
            throw new ArgumentException("Start and end dates must be supplied together.");
        return job.ExecuteAsync(start.Value, end.Value, execution, ct);
    }
}
