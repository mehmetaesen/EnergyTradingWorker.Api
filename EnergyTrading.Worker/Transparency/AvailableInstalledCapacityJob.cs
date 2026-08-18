using EnergyTrading.Application;
using Hangfire;
using Hangfire.Server;

namespace EnergyTrading.Worker.Transparency;

public sealed class AvailableInstalledCapacityJob(
    EnergyTrading.Application.Transparency.AvailableInstalledCapacityJob job
) : JobAdapter<GenerationPlanItem>(job)
{
    [
        Queue("transparency"),
        AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900]),
        DisableConcurrentExecution(60)
    ]
    public Task ExecuteAsync(DateOnly? s, DateOnly? e, PerformContext? c, CancellationToken ct) =>
        ExecuteCoreAsync(s, e, c, ct);
}
