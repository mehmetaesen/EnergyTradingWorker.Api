using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class FinalGenerationPlanJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<FinalGenerationPlan> r,
    IUnitOfWork u,
    ITurkeyClock k,
    ITransparencyRegionProvider p
) : PeriodDataJobBase<FinalGenerationPlan, GenerationPlanItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_FINAL_GENERATION_PLAN";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today, k.Today);

    protected override async Task<IReadOnlyList<GenerationPlanItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        var x = TransparencyPeriod.Range(s, e);
        return (
            await c.GetDataAsync<GenerationPlanResponse>(
                "v1/generation/data/sbfgp",
                new GenerationPlanRequest(x.Start, x.End, p.SystemMarginalPriceRegion),
                ct
            )
        ).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(GenerationPlanItem x) =>
        (DateOnly.FromDateTime(x.Date.Date), TransparencyPeriod.Hour(x.Time));

    protected override void Map(GenerationPlanItem x, FinalGenerationPlan y) =>
        (
            y.River,
            y.Dam,
            y.Biomass,
            y.Other,
            y.NaturalGas,
            y.FuelOil,
            y.Solar,
            y.ImportedCoal,
            y.Geothermal,
            y.Lignite,
            y.Naphtha,
            y.Wind,
            y.HardCoal,
            y.Total
        ) = (
            x.River,
            x.Dam,
            x.Biomass,
            x.Other,
            x.NaturalGas,
            x.FuelOil,
            x.Solar,
            x.ImportedCoal,
            x.Geothermal,
            x.Lignite,
            x.Naphtha,
            x.Wind,
            x.HardCoal,
            x.Total
        );

}
