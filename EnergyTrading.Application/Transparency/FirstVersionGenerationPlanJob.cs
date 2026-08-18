using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class FirstVersionGenerationPlanJob(
    IIntegrationJobLogService logs,
    ITransparencyApiClient client,
    IGenericRepository<FirstVersionGenerationPlan> repo,
    IUnitOfWork uow,
    ITurkeyClock clock
) : PeriodDataJobBase<FirstVersionGenerationPlan, GenerationPlanItem>(logs, repo, uow)
{
    public const string Code = "TRANSPARENCY_KGUP_FIRST_VERSION";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (clock.Today.AddDays(1), clock.Today.AddDays(1));

    protected override async Task<IReadOnlyList<GenerationPlanItem>> FetchAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken ct
    )
    {
        var r = TransparencyPeriod.Range(start, end);
        return (await client.GetGenerationPlanAsync(new(r.Start, r.End), true, ct)).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(GenerationPlanItem x) =>
        (DateOnly.FromDateTime(x.Date.DateTime), TransparencyPeriod.Hour(x.Time));

    protected override void Map(GenerationPlanItem x, FirstVersionGenerationPlan e) =>
        GenerationPlanMapping.Map(x, e);

    protected override bool HasChanges(GenerationPlanItem x, FirstVersionGenerationPlan e) =>
        GenerationPlanMapping.Changed(x, e);
}
