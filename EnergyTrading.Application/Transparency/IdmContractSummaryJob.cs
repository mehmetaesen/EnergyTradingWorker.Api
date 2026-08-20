using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class IdmContractSummaryJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<IdmContractSummary> r,
    IUnitOfWork u,
    ITurkeyClock k
) : KeyedPeriodDataJobBase<IdmContractSummary, IdmContractSummaryItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_IDM_CONTRACT_SUMMARY";
    private DateOnly requestDate;
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today, k.Today);

    protected override async Task<IReadOnlyList<IdmContractSummaryItem>> FetchAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken ct)
    {
        requestDate = start;
        var range = TransparencyPeriod.Range(start, end);
        return (await c.GetDataAsync<IdmContractSummaryResponse>(
            "/reporting-service/v1/data/idm-contract-summary",
            new DateRangeRequest(range.Start, range.End),
            ct)).Items;
    }

    protected override (DateOnly Date, int Period, string ExternalKey) GetKey(
        IdmContractSummaryItem item) =>
        (
            requestDate,
            TransparencyPeriod.ContractPeriod(item.ContractName),
            $"{item.ContractName ?? string.Empty}|{item.ContractTypeDescription ?? string.Empty}"
        );

    protected override void Map(IdmContractSummaryItem source, IdmContractSummary target) =>
        (
            target.ContractName,
            target.ContractTypeDescription,
            target.WeightedAveragePrice,
            target.MatchingQuantity,
            target.TradingVolume,
            target.MaximumBidPrice,
            target.MaximumMatchingPrice,
            target.MaximumAskPrice,
            target.MinimumBidPrice,
            target.MinimumMatchingPrice,
            target.MinimumAskPrice,
            target.BidQuantity,
            target.AskQuantity
        ) = (
            source.ContractName,
            source.ContractTypeDescription,
            source.WeightedAveragePrice,
            source.MatchingQuantity,
            source.TradingVolume,
            source.MaximumBidPrice,
            source.MaximumMatchingPrice,
            source.MaximumAskPrice,
            source.MinimumBidPrice,
            source.MinimumMatchingPrice,
            source.MinimumAskPrice,
            source.BidQuantity,
            source.AskQuantity
        );
}
