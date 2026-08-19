using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class SystemDirectionJob(
    IIntegrationJobLogService logs,
    ITransparencyApiClient client,
    IGenericRepository<SystemDirection> repo,
    IUnitOfWork uow,
    ITurkeyClock clock,
    ITransparencyRegionProvider region
) : PeriodDataJobBase<SystemDirection, SystemDirectionItem>(logs, repo, uow)
{
    public const string Code = "TRANSPARENCY_SYSTEM_DIRECTION";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (clock.Today, clock.Today);

    protected override async Task<IReadOnlyList<SystemDirectionItem>> FetchAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken ct
    )
    {
        var r = TransparencyPeriod.Range(start, end);
        return (
            await client.GetSystemDirectionAsync(
                new(r.Start, r.End, region.SystemMarginalPriceRegion),
                ct
            )
        ).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(SystemDirectionItem x) =>
        (DateOnly.FromDateTime(x.Date.DateTime), TransparencyPeriod.Hour(x.Hour));

    protected override void Map(SystemDirectionItem x, SystemDirection e)
    {
        e.DirectionId = x.SmpDirectionId;
        e.Direction = x.SystemDirection ?? string.Empty;
    }

}
