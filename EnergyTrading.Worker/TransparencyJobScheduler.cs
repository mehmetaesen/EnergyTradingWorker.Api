using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Server;
using Scheduled = EnergyTrading.Worker.Transparency;
using ApplicationJobs = EnergyTrading.Application.Transparency;

namespace EnergyTrading.Worker;

public static class TransparencyJobScheduler
{
    private static readonly IReadOnlySet<string> DailyBackfillJobCodes = new HashSet<string>
    {
        ApplicationJobs.NewInstalledCapacityJob.Code,
        ApplicationJobs.SgpPriceJob.Code,
        ApplicationJobs.IdmMatchingQuantityJob.Code,
        ApplicationJobs.IdmContractSummaryJob.Code,
    };

    public static readonly IReadOnlySet<string> SupportedJobCodes = new HashSet<string>
    {
        ApplicationJobs.MarketClearingPriceJob.Code,
        ApplicationJobs.SystemMarginalPriceJob.Code,
        ApplicationJobs.LoadEstimationPlanJob.Code,
        ApplicationJobs.RealTimeConsumptionJob.Code,
        ApplicationJobs.GenerationPlanJob.Code,
        ApplicationJobs.FirstVersionGenerationPlanJob.Code,
        ApplicationJobs.InjectionQuantityJob.Code,
        ApplicationJobs.PrimaryFrequencyCapacityPriceJob.Code,
        ApplicationJobs.SecondaryFrequencyCapacityPriceJob.Code,
        ApplicationJobs.SystemDirectionJob.Code,
        ApplicationJobs.WindGenerationForecastJob.Code,
        ApplicationJobs.FinalGenerationPlanJob.Code,
        ApplicationJobs.NewInstalledCapacityJob.Code,
        ApplicationJobs.PlannedPowerOutageJob.Code,
        ApplicationJobs.UnplannedPowerOutageJob.Code,
        ApplicationJobs.SgpPriceJob.Code,
        ApplicationJobs.AvailableInstalledCapacityJob.Code,
        ApplicationJobs.UnlicensedGenerationJob.Code,
        ApplicationJobs.RealTimeGenerationJob.Code,
        ApplicationJobs.OrderSummaryUpJob.Code,
        ApplicationJobs.OrderSummaryDownJob.Code,
        ApplicationJobs.ClearingQuantityJob.Code,
        ApplicationJobs.IdmWeightedAveragePriceJob.Code,
        ApplicationJobs.IdmMatchingQuantityJob.Code,
        ApplicationJobs.WithdrawalQuantityJob.Code,
        ApplicationJobs.IdmContractSummaryJob.Code,
    };

    public static string? Enqueue(
        IBackgroundJobClient jobs,
        string jobCode,
        DateOnly startDate,
        DateOnly endDate,
        string? parentJobId = null) =>
        jobCode switch
        {
            ApplicationJobs.MarketClearingPriceJob.Code => Add<Scheduled.MarketClearingPriceJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.SystemMarginalPriceJob.Code => Add<Scheduled.SystemMarginalPriceJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.LoadEstimationPlanJob.Code => Add<Scheduled.LoadEstimationPlanJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.RealTimeConsumptionJob.Code => Add<Scheduled.RealTimeConsumptionJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.GenerationPlanJob.Code => Add<Scheduled.GenerationPlanJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.FirstVersionGenerationPlanJob.Code => Add<Scheduled.FirstVersionGenerationPlanJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.InjectionQuantityJob.Code => Add<Scheduled.InjectionQuantityJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.PrimaryFrequencyCapacityPriceJob.Code => Add<Scheduled.PrimaryFrequencyCapacityPriceJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.SecondaryFrequencyCapacityPriceJob.Code => Add<Scheduled.SecondaryFrequencyCapacityPriceJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.SystemDirectionJob.Code => Add<Scheduled.SystemDirectionJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.WindGenerationForecastJob.Code => Add<Scheduled.WindGenerationForecastJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.FinalGenerationPlanJob.Code => Add<Scheduled.FinalGenerationPlanJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.NewInstalledCapacityJob.Code => Add<Scheduled.NewInstalledCapacityJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.PlannedPowerOutageJob.Code => Add<Scheduled.PlannedPowerOutageJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.UnplannedPowerOutageJob.Code => Add<Scheduled.UnplannedPowerOutageJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.SgpPriceJob.Code => Add<Scheduled.SgpPriceJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.AvailableInstalledCapacityJob.Code => Add<Scheduled.AvailableInstalledCapacityJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.UnlicensedGenerationJob.Code => Add<Scheduled.UnlicensedGenerationJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.RealTimeGenerationJob.Code => Add<Scheduled.RealTimeGenerationJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.OrderSummaryUpJob.Code => Add<Scheduled.OrderSummaryUpJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.OrderSummaryDownJob.Code => Add<Scheduled.OrderSummaryDownJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.ClearingQuantityJob.Code => Add<Scheduled.ClearingQuantityJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.IdmWeightedAveragePriceJob.Code => Add<Scheduled.IdmWeightedAveragePriceJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.IdmMatchingQuantityJob.Code => Add<Scheduled.IdmMatchingQuantityJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.WithdrawalQuantityJob.Code => Add<Scheduled.WithdrawalQuantityJob>(jobs, startDate, endDate, parentJobId),
            ApplicationJobs.IdmContractSummaryJob.Code => Add<Scheduled.IdmContractSummaryJob>(jobs, startDate, endDate, parentJobId),
            _ => null,
        };

    public static int GetBackfillChunkSizeInDays(string jobCode) =>
        DailyBackfillJobCodes.Contains(jobCode) ? 1 : 31;

    private static string Add<TJob>(
        IBackgroundJobClient jobs,
        DateOnly startDate,
        DateOnly endDate,
        string? parentJobId)
    {
        var method = typeof(TJob).GetMethod(
            "ExecuteAsync",
            [typeof(DateOnly?), typeof(DateOnly?), typeof(PerformContext), typeof(CancellationToken)])
            ?? throw new InvalidOperationException($"{typeof(TJob).Name}.ExecuteAsync method was not found.");
        var job = new Job(
            typeof(TJob),
            method,
            startDate,
            endDate,
            null,
            CancellationToken.None);
        var nextState = new EnqueuedState("transparency");
        return jobs.Create(
            job,
            parentJobId is null
                ? nextState
                : new AwaitingState(
                    parentJobId,
                    nextState,
                    JobContinuationOptions.OnAnyFinishedState));
    }
}
