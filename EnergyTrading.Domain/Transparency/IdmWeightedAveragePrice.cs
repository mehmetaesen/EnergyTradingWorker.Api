using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class IdmWeightedAveragePrice : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public decimal WeightedAveragePrice { get; set; }
}
