using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class WithdrawalQuantity : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public DateTimeOffset Period { get; set; }
    public decimal Swv { get; set; }
}
