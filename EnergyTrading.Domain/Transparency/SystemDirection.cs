using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class SystemDirection : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public long? DirectionId { get; set; }
    public string Direction { get; set; } = string.Empty;
}
