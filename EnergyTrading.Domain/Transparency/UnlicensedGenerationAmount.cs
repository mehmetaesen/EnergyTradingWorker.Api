using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class UnlicensedGenerationAmount : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public decimal Biogas { get; set; }
    public decimal Biomass { get; set; }
    public decimal Other { get; set; }
    public decimal Solar { get; set; }
    public decimal ChannelType { get; set; }
    public decimal Wind { get; set; }
    public decimal Total { get; set; }
}
