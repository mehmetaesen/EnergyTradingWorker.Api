using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class IdmContractSummary : BaseEntity, IExternalKeyEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public string ExternalKey { get; set; } = string.Empty;
    public string? ContractName { get; set; }
    public string? ContractTypeDescription { get; set; }
    public decimal? WeightedAveragePrice { get; set; }
    public decimal? MatchingQuantity { get; set; }
    public decimal? TradingVolume { get; set; }
    public decimal? MaximumBidPrice { get; set; }
    public decimal? MaximumMatchingPrice { get; set; }
    public decimal? MaximumAskPrice { get; set; }
    public decimal? MinimumBidPrice { get; set; }
    public decimal? MinimumMatchingPrice { get; set; }
    public decimal? MinimumAskPrice { get; set; }
    public decimal? BidQuantity { get; set; }
    public decimal? AskQuantity { get; set; }
}
