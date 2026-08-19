using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class RealTimeGenerationJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<RealTimeGeneration> r,
    IUnitOfWork u,
    ITurkeyClock k
) : PeriodDataJobBase<RealTimeGeneration, RealTimeGenerationItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_REALTIME_GENERATION";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today.AddDays(-1), k.Today.AddDays(-1));

    protected override async Task<IReadOnlyList<RealTimeGenerationItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        var x = TransparencyPeriod.Range(s, e);
        return (
            await c.GetDataAsync<RealTimeGenerationResponse>(
                "v1/generation/data/realtime-generation",
                new DateRangeRequest(x.Start, x.End),
                ct
            )
        ).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(RealTimeGenerationItem x) =>
        (DateOnly.FromDateTime(x.Date.Date), TransparencyPeriod.Hour(x.Hour));

    protected override void Map(RealTimeGenerationItem x, RealTimeGeneration y) =>
        (
            y.AsphaltiteCoal,
            y.Biomass,
            y.BlackCoal,
            y.DammedHydro,
            y.Fueloil,
            y.Geothermal,
            y.ImportCoal,
            y.ImportExport,
            y.Lignite,
            y.Lng,
            y.Naphta,
            y.NaturalGas,
            y.River,
            y.Sun,
            y.Total,
            y.Wasteheat,
            y.Wind
        ) = (
            x.AsphaltiteCoal,
            x.Biomass,
            x.BlackCoal,
            x.DammedHydro,
            x.Fueloil,
            x.Geothermal,
            x.ImportCoal,
            x.ImportExport,
            x.Lignite,
            x.Lng,
            x.Naphta,
            x.NaturalGas,
            x.River,
            x.Sun,
            x.Total,
            x.Wasteheat,
            x.Wind
        );

}
