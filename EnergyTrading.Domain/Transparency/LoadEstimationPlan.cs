using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class LoadEstimationPlan : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public decimal LoadEstimation { get; set; }
}
