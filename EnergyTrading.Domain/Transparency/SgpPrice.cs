using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class SgpPrice : BaseEntity, IRawTransparencyEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public string Payload { get; set; } = "{}";
}
