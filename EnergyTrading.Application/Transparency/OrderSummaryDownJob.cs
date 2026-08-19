using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class OrderSummaryDownJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<DownRegulationOrderSummary> r,
    IUnitOfWork u,
    ITurkeyClock k,
    ITransparencyRegionProvider p
) : PeriodDataJobBase<DownRegulationOrderSummary, OrderSummaryDownItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_ORDER_SUMMARY_DOWN";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today, k.Today);

    protected override async Task<IReadOnlyList<OrderSummaryDownItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        var x = TransparencyPeriod.Range(s, e);
        return (
            await c.GetDataAsync<OrderSummaryDownResponse>(
                "v1/markets/bpm/data/order-summary-down",
                new SystemDirectionRequest(x.Start, x.End, p.SystemMarginalPriceRegion),
                ct
            )
        ).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(OrderSummaryDownItem x) =>
        (DateOnly.FromDateTime(x.Date.Date), TransparencyPeriod.Hour(x.Hour));

    protected override void Map(OrderSummaryDownItem x, DownRegulationOrderSummary y) =>
        (
            y.Net,
            y.DownRegulationDelivered,
            y.DownRegulationOneCoded,
            y.DownRegulationTwoCoded,
            y.DownRegulationZeroCoded
        ) = (
            x.Net,
            x.DownRegulationDelivered,
            x.DownRegulationOneCoded,
            x.DownRegulationTwoCoded,
            x.DownRegulationZeroCoded
        );

}
