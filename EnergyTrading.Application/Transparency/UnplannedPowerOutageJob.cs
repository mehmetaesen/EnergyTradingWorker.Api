using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class UnplannedPowerOutageJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<UnplannedPowerOutage> r,
    IUnitOfWork u,
    ITurkeyClock k,
    ITransparencyRegionProvider p
) : KeyedPeriodDataJobBase<UnplannedPowerOutage, PowerOutageItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_UNPLANNED_POWER_OUTAGE";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today, k.Today);

    protected override async Task<IReadOnlyList<PowerOutageItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    ) =>
        (
            await c.GetDataAsync<PowerOutageResponse>(
                "v1/consumption/data/unplanned-power-outage-info",
                new
                {
                    period = new DateTimeOffset(
                        s.ToDateTime(TimeOnly.MinValue),
                        TransparencyPeriod.TurkeyOffset
                    ),
                },
                ct
            )
        ).Items;

    protected override (DateOnly Date, int Period, string ExternalKey) GetKey(PowerOutageItem x) =>
        (DateOnly.FromDateTime(x.Date.Date), x.StartTime.Hour + 1, x.Id.ToString());

    protected override void Map(PowerOutageItem x, UnplannedPowerOutage y) =>
        (
            y.DistributionCompanyName,
            y.District,
            y.AffectedNeighbourhoods,
            y.AffectedSubscribers,
            y.EndTime,
            y.HourlyLoadAverage,
            y.OutageId,
            y.Province,
            y.Reason,
            y.StartTime
        ) = (
            x.DistributionCompanyName,
            x.District,
            x.EffectedNeighbourhoods,
            x.EffectedSubscribers,
            x.EndTime,
            x.HourlyLoadAvg,
            x.Id,
            x.Province,
            x.Reason,
            x.StartTime
        );

    protected override bool HasChanges(PowerOutageItem x, UnplannedPowerOutage y) =>
        x.EndTime != y.EndTime
        || x.StartTime != y.StartTime
        || x.EffectedSubscribers != y.AffectedSubscribers
        || x.Reason != y.Reason;
}
