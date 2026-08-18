using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class LoadEstimationPlanJob(
    IIntegrationJobLogService logs,
    ITransparencyApiClient client,
    IGenericRepository<LoadEstimationPlan> repo,
    IUnitOfWork uow,
    ITurkeyClock clock
) : PeriodDataJobBase<LoadEstimationPlan, LoadEstimationPlanItem>(logs, repo, uow)
{
    public const string Code = "TRANSPARENCY_LOAD_ESTIMATION_PLAN";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (clock.Today.AddDays(1), clock.Today.AddDays(1));

    protected override async Task<IReadOnlyList<LoadEstimationPlanItem>> FetchAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken ct
    )
    {
        var range = TransparencyPeriod.Range(start, end);
        return (await client.GetLoadEstimationPlanAsync(new(range.Start, range.End), ct)).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(LoadEstimationPlanItem x) =>
        (DateOnly.FromDateTime(x.Date.DateTime), x.Time.Hour + 1);

    protected override void Map(LoadEstimationPlanItem x, LoadEstimationPlan e) =>
        e.LoadEstimation = x.Lep;

    protected override bool HasChanges(LoadEstimationPlanItem x, LoadEstimationPlan e) =>
        e.LoadEstimation != x.Lep;
}
