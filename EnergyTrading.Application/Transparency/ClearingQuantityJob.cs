using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class ClearingQuantityJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<ClearingQuantity> r,
    IUnitOfWork u,
    ITurkeyClock k
) : PeriodDataJobBase<ClearingQuantity, ClearingQuantityItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_CLEARING_QUANTITY";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today.AddDays(1), k.Today.AddDays(1));

    protected override async Task<IReadOnlyList<ClearingQuantityItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        var x = TransparencyPeriod.Range(s, e);
        return (
            await c.GetDataAsync<ClearingQuantityResponse>(
                "v1/markets/dam/data/clearing-quantity",
                new DateRangeRequest(x.Start, x.End),
                ct
            )
        ).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(ClearingQuantityItem x) =>
        (DateOnly.FromDateTime(x.Date.Date), TransparencyPeriod.Hour(x.Hour));

    protected override void Map(ClearingQuantityItem x, ClearingQuantity y) =>
        (y.MatchedBids, y.MatchedOffers) = (x.MatchedBids, x.MatchedOffers);

}
