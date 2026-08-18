using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class RealTimeGeneration : BaseEntity, IPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public decimal AsphaltiteCoal { get; set; }
    public decimal Biomass { get; set; }
    public decimal BlackCoal { get; set; }
    public decimal DammedHydro { get; set; }
    public decimal Fueloil { get; set; }
    public decimal Geothermal { get; set; }
    public decimal ImportCoal { get; set; }
    public decimal ImportExport { get; set; }
    public decimal Lignite { get; set; }
    public decimal Lng { get; set; }
    public decimal Naphta { get; set; }
    public decimal NaturalGas { get; set; }
    public decimal River { get; set; }
    public decimal Sun { get; set; }
    public decimal Total { get; set; }
    public decimal Wasteheat { get; set; }
    public decimal Wind { get; set; }
}
