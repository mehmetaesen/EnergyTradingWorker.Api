using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class IdmMatchingQuantity : BaseEntity, IExternalKeyEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public string ExternalKey { get; set; } = string.Empty;
    public decimal ClearingQuantityAsk { get; set; }
    public decimal ClearingQuantityBid { get; set; }
    public string ContractName { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
}
