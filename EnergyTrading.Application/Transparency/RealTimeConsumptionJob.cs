using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class RealTimeConsumptionJob(
    IIntegrationJobLogService logs,
    ITransparencyApiClient client,
    IGenericRepository<RealTimeConsumption> repo,
    IUnitOfWork uow,
    ITurkeyClock clock
) : PeriodDataJobBase<RealTimeConsumption, RealTimeConsumptionItem>(logs, repo, uow)
{
    public const string Code = "TRANSPARENCY_REALTIME_CONSUMPTION";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (clock.Today, clock.Today);

    protected override async Task<IReadOnlyList<RealTimeConsumptionItem>> FetchAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken ct
    )
    {
        var range = TransparencyPeriod.Range(start, end);
        return (await client.GetRealTimeConsumptionAsync(new(range.Start, range.End), ct)).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(RealTimeConsumptionItem x) =>
        (DateOnly.FromDateTime(x.Date.DateTime), x.Time.Hour + 1);

    protected override void Map(RealTimeConsumptionItem x, RealTimeConsumption e) =>
        e.Consumption = x.Consumption;

    protected override bool HasChanges(RealTimeConsumptionItem x, RealTimeConsumption e) =>
        e.Consumption != x.Consumption;
}
