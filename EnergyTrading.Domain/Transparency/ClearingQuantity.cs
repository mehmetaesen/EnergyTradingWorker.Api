using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class ClearingQuantity : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public decimal MatchedBids { get; set; }
    public decimal MatchedOffers { get; set; }
}
