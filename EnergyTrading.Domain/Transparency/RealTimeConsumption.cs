using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class RealTimeConsumption : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public decimal Consumption { get; set; }
}
