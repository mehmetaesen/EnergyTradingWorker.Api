using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class NewInstalledCapacity : BaseEntity, IExternalKeyEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public string ExternalKey { get; set; } = string.Empty;
    public string RenewableEnergyType { get; set; } = string.Empty;
    public decimal LicensedCapacity { get; set; }
    public decimal UnlicensedCapacity { get; set; }
    public decimal Total { get; set; }
}
