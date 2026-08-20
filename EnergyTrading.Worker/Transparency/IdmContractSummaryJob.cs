using EnergyTrading.Application;
using Hangfire;
using Hangfire.Server;

namespace EnergyTrading.Worker.Transparency;

public sealed class IdmContractSummaryJob(
    EnergyTrading.Application.Transparency.IdmContractSummaryJob job
) : JobAdapter<IdmContractSummaryItem>(job)
{
    [
        Queue("transparency"),
        AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900]),
        DisableConcurrentExecution(60)
    ]
    public Task ExecuteAsync(DateOnly? s, DateOnly? e, PerformContext? c, CancellationToken ct) =>
        ExecuteCoreAsync(s, e, c, ct);
}
