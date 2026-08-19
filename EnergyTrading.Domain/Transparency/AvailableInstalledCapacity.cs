using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class AvailableInstalledCapacity : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public decimal River { get; set; }
    public decimal Dam { get; set; }
    public decimal Biomass { get; set; }
    public decimal Other { get; set; }
    public decimal NaturalGas { get; set; }
    public decimal FuelOil { get; set; }
    public decimal Solar { get; set; }
    public decimal ImportedCoal { get; set; }
    public decimal Geothermal { get; set; }
    public decimal Lignite { get; set; }
    public decimal Naphtha { get; set; }
    public decimal Wind { get; set; }
    public decimal HardCoal { get; set; }
    public decimal? Total { get; set; }
}
