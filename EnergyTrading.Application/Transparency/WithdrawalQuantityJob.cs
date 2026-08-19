using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class WithdrawalQuantityJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<WithdrawalQuantity> r,
    IUnitOfWork u,
    ITurkeyClock k,
    ITransparencyRegionProvider p
) : PeriodDataJobBase<WithdrawalQuantity, WithdrawalQuantityItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_UECM";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today.AddDays(-1), k.Today.AddDays(-1));

    protected override async Task<IReadOnlyList<WithdrawalQuantityItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        var x = TransparencyPeriod.Range(s, e);
        return (
            await c.GetDataAsync<WithdrawalQuantityResponse>(
                "v1/consumption/data/uecm",
                new SystemDirectionRequest(x.Start, x.End, p.SystemMarginalPriceRegion),
                ct
            )
        ).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(WithdrawalQuantityItem x) =>
        (DateOnly.FromDateTime(x.Hour.Date), x.Hour.Hour + 1);

    protected override void Map(WithdrawalQuantityItem x, WithdrawalQuantity y) =>
        (y.Period, y.Swv) = (x.Period, x.Swv);

}
