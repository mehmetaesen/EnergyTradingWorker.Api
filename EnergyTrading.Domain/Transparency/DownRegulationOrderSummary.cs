using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class DownRegulationOrderSummary : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public decimal Net { get; set; }
    public decimal DownRegulationDelivered { get; set; }
    public decimal DownRegulationOneCoded { get; set; }
    public decimal DownRegulationTwoCoded { get; set; }
    public decimal DownRegulationZeroCoded { get; set; }
}
