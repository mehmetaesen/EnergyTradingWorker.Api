using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class SystemMarginalPrice : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public decimal Price { get; set; }
}
