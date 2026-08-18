using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

internal static class GenerationPlanMapping
{
    internal static bool Changed(GenerationPlanItem x, GenerationPlan e) =>
        e.Total != x.Total
        || e.River != x.River
        || e.Dam != x.Dam
        || e.Biomass != x.Biomass
        || e.Other != x.Other
        || e.NaturalGas != x.NaturalGas
        || e.FuelOil != x.FuelOil
        || e.Solar != x.Solar
        || e.ImportedCoal != x.ImportedCoal
        || e.Geothermal != x.Geothermal
        || e.Lignite != x.Lignite
        || e.Naphtha != x.Naphtha
        || e.Wind != x.Wind
        || e.HardCoal != x.HardCoal;

    internal static void Map(GenerationPlanItem x, GenerationPlan e)
    {
        e.River = x.River;
        e.Dam = x.Dam;
        e.Biomass = x.Biomass;
        e.Other = x.Other;
        e.NaturalGas = x.NaturalGas;
        e.FuelOil = x.FuelOil;
        e.Solar = x.Solar;
        e.ImportedCoal = x.ImportedCoal;
        e.Geothermal = x.Geothermal;
        e.Lignite = x.Lignite;
        e.Naphtha = x.Naphtha;
        e.Wind = x.Wind;
        e.HardCoal = x.HardCoal;
        e.Total = x.Total;
    }

    internal static bool Changed(GenerationPlanItem x, FirstVersionGenerationPlan e) =>
        e.Total != x.Total
        || e.River != x.River
        || e.Dam != x.Dam
        || e.Biomass != x.Biomass
        || e.Other != x.Other
        || e.NaturalGas != x.NaturalGas
        || e.FuelOil != x.FuelOil
        || e.Solar != x.Solar
        || e.ImportedCoal != x.ImportedCoal
        || e.Geothermal != x.Geothermal
        || e.Lignite != x.Lignite
        || e.Naphtha != x.Naphtha
        || e.Wind != x.Wind
        || e.HardCoal != x.HardCoal;

    internal static void Map(GenerationPlanItem x, FirstVersionGenerationPlan e)
    {
        e.River = x.River;
        e.Dam = x.Dam;
        e.Biomass = x.Biomass;
        e.Other = x.Other;
        e.NaturalGas = x.NaturalGas;
        e.FuelOil = x.FuelOil;
        e.Solar = x.Solar;
        e.ImportedCoal = x.ImportedCoal;
        e.Geothermal = x.Geothermal;
        e.Lignite = x.Lignite;
        e.Naphtha = x.Naphtha;
        e.Wind = x.Wind;
        e.HardCoal = x.HardCoal;
        e.Total = x.Total;
    }
}
