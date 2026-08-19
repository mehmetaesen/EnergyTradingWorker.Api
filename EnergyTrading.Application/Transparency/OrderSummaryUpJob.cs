using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class OrderSummaryUpJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<UpRegulationOrderSummary> r,
    IUnitOfWork u,
    ITurkeyClock k,
    ITransparencyRegionProvider p
) : PeriodDataJobBase<UpRegulationOrderSummary, OrderSummaryUpItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_ORDER_SUMMARY_UP";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today, k.Today);

    protected override async Task<IReadOnlyList<OrderSummaryUpItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        var x = TransparencyPeriod.Range(s, e);
        return (
            await c.GetDataAsync<OrderSummaryUpResponse>(
                "v1/markets/bpm/data/order-summary-up",
                new SystemDirectionRequest(x.Start, x.End, p.SystemMarginalPriceRegion),
                ct
            )
        ).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(OrderSummaryUpItem x) =>
        (DateOnly.FromDateTime(x.Date.Date), TransparencyPeriod.Hour(x.Hour));

    protected override void Map(OrderSummaryUpItem x, UpRegulationOrderSummary y) =>
        (
            y.Net,
            y.UpRegulationDelivered,
            y.UpRegulationOneCoded,
            y.UpRegulationTwoCoded,
            y.UpRegulationZeroCoded
        ) = (
            x.Net,
            x.UpRegulationDelivered,
            x.UpRegulationOneCoded,
            x.UpRegulationTwoCoded,
            x.UpRegulationZeroCoded
        );

}
