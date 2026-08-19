using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class AvailableInstalledCapacityJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<AvailableInstalledCapacity> r,
    IUnitOfWork u,
    ITurkeyClock k,
    ITransparencyRegionProvider p
) : PeriodDataJobBase<AvailableInstalledCapacity, GenerationPlanItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_AVAILABLE_INSTALLED_CAPACITY";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today, k.Today);

    protected override async Task<IReadOnlyList<GenerationPlanItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        if (s > k.Today)
            throw new ArgumentOutOfRangeException(
                nameof(s),
                "Emre amade kapasite verisi gelecek tarih için alınamaz."
            );

        var safeEnd = e > k.Today ? k.Today : e;
        var x = TransparencyPeriod.Range(s, safeEnd);
        return (
            await c.GetDataAsync<GenerationPlanResponse>(
                "v1/generation/data/aic",
                new GenerationPlanRequest(x.Start, x.End, p.SystemMarginalPriceRegion),
                ct
            )
        ).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(GenerationPlanItem x) =>
        (DateOnly.FromDateTime(x.Date.Date), TransparencyPeriod.Hour(x.Time));

    protected override void Map(GenerationPlanItem x, AvailableInstalledCapacity y) =>
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

    protected override bool HasChanges(
        GenerationPlanItem x,
        AvailableInstalledCapacity y
    ) =>
        x.River != y.River
        || x.Dam != y.Dam
        || x.Biomass != y.Biomass
        || x.Other != y.Other
        || x.NaturalGas != y.NaturalGas
        || x.FuelOil != y.FuelOil
        || x.Solar != y.Solar
        || x.ImportedCoal != y.ImportedCoal
        || x.Geothermal != y.Geothermal
        || x.Lignite != y.Lignite
        || x.Naphtha != y.Naphtha
        || x.Wind != y.Wind
        || x.HardCoal != y.HardCoal
        || x.Total != y.Total;
}
