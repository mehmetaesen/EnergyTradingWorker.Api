using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class IdmMatchingQuantityJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<IdmMatchingQuantity> r,
    IUnitOfWork u,
    ITurkeyClock k
) : KeyedPeriodDataJobBase<IdmMatchingQuantity, MatchingQuantityItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_IDM_MATCHING_QUANTITY";
    private DateOnly requestDate;
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today.AddDays(-1), k.Today.AddDays(-1));

    protected override async Task<IReadOnlyList<MatchingQuantityItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        var latestAvailableDate = k.Today.AddDays(-1);
        var effectiveStart = s > latestAvailableDate ? latestAvailableDate : s;
        var effectiveEnd = e > latestAvailableDate ? latestAvailableDate : e;
        requestDate = effectiveStart;
        var x = TransparencyPeriod.Range(effectiveStart, effectiveEnd);
        return (
            await c.GetDataAsync<MatchingQuantityResponse>(
                "v1/markets/idm/data/matching-quantity",
                new DateRangeRequest(x.Start, x.End),
                ct
            )
        ).Items;
    }

    protected override (DateOnly Date, int Period, string ExternalKey) GetKey(
        MatchingQuantityItem x
    ) => (
        requestDate,
        TransparencyPeriod.ContractPeriod(x.KontratAdi),
        $"{x.KontratAdi}|{x.KontratTuru}"
    );

    protected override void Map(MatchingQuantityItem x, IdmMatchingQuantity y) =>
        (y.ClearingQuantityAsk, y.ClearingQuantityBid, y.ContractName, y.ContractType) = (
            x.ClearingQuantityAsk,
            x.ClearingQuantityBid,
            x.KontratAdi,
            x.KontratTuru
        );

}
