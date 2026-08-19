using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class IdmWeightedAveragePriceJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<IdmWeightedAveragePrice> r,
    IUnitOfWork u,
    ITurkeyClock k
) : PeriodDataJobBase<IdmWeightedAveragePrice, WeightedAveragePriceItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_IDM_WEIGHTED_AVERAGE_PRICE";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today, k.Today);

    protected override async Task<IReadOnlyList<WeightedAveragePriceItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        var x = TransparencyPeriod.Range(s, e);
        return (
            await c.GetDataAsync<WeightedAveragePriceResponse>(
                "v1/markets/idm/data/weighted-average-price",
                new DateRangeRequest(x.Start, x.End),
                ct
            )
        ).Items.Where(item => item.Wap.HasValue).ToList();
    }

    protected override (DateOnly Date, int Period) GetKey(WeightedAveragePriceItem x) =>
        (DateOnly.FromDateTime(x.Date.Date), TransparencyPeriod.Hour(x.Hour));

    protected override void Map(WeightedAveragePriceItem x, IdmWeightedAveragePrice y) =>
        y.WeightedAveragePrice = x.Wap!.Value;

}
