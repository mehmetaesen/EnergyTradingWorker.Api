using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class PlannedPowerOutageJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<PlannedPowerOutage> r,
    IUnitOfWork u,
    ITurkeyClock k
) : KeyedPeriodDataJobBase<PlannedPowerOutage, PowerOutageItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_PLANNED_POWER_OUTAGE";
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
                "v1/consumption/data/planned-power-outage-info",
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

    protected override void Map(PowerOutageItem x, PlannedPowerOutage y) =>
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
            x.EndTime.ToUniversalTime(),
            x.HourlyLoadAvg,
            x.Id,
            x.Province,
            x.Reason,
            x.StartTime.ToUniversalTime()
        );

}
