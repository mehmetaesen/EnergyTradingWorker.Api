using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class UpRegulationOrderSummary : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public decimal Net { get; set; }
    public decimal UpRegulationDelivered { get; set; }
    public decimal UpRegulationOneCoded { get; set; }
    public decimal UpRegulationTwoCoded { get; set; }
    public decimal UpRegulationZeroCoded { get; set; }
}
