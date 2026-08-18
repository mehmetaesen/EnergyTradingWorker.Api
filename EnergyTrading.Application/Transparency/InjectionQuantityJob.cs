using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class InjectionQuantityJob(
    IIntegrationJobLogService logs,
    ITransparencyApiClient client,
    IGenericRepository<InjectionQuantity> repo,
    IUnitOfWork uow,
    ITurkeyClock clock
) : PeriodDataJobBase<InjectionQuantity, InjectionQuantityItem>(logs, repo, uow)
{
    public const string Code = "TRANSPARENCY_UEVM";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (clock.Today.AddDays(-1), clock.Today.AddDays(-1));

    protected override async Task<IReadOnlyList<InjectionQuantityItem>> FetchAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken ct
    )
    {
        var r = TransparencyPeriod.Range(start, end);
        return (await client.GetInjectionQuantityAsync(new(r.Start, r.End), ct)).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(InjectionQuantityItem x) =>
        (DateOnly.FromDateTime(x.Date.DateTime), TransparencyPeriod.Hour(x.Hour));

    protected override void Map(InjectionQuantityItem x, InjectionQuantity e)
    {
        e.Asphaltite = x.Asphaltite;
        e.Biomass = x.Biomass;
        e.Dam = x.Dam;
        e.FuelOil = x.Fueloil;
        e.Geothermal = x.Geothermal;
        e.ImportedCoal = x.ImportedCoal;
        e.InternationalExport = x.InternationalExport;
        e.InternationalImport = x.InternationalImport;
        e.Lignite = x.Lignite;
        e.Lng = x.Lng;
        e.Naphtha = x.Naphtha;
        e.NaturalGas = x.NaturalGas;
        e.Other = x.Other;
        e.River = x.River;
        e.HardCoal = x.StoneCoal;
        e.Solar = x.Sun;
        e.Wind = x.Wind;
        e.Total = x.Total;
    }

    protected override bool HasChanges(InjectionQuantityItem x, InjectionQuantity e) =>
        e.Total != x.Total
        || e.Asphaltite != x.Asphaltite
        || e.Biomass != x.Biomass
        || e.Dam != x.Dam
        || e.FuelOil != x.Fueloil
        || e.Geothermal != x.Geothermal
        || e.ImportedCoal != x.ImportedCoal
        || e.InternationalExport != x.InternationalExport
        || e.InternationalImport != x.InternationalImport
        || e.Lignite != x.Lignite
        || e.Lng != x.Lng
        || e.Naphtha != x.Naphtha
        || e.NaturalGas != x.NaturalGas
        || e.Other != x.Other
        || e.River != x.River
        || e.HardCoal != x.StoneCoal
        || e.Solar != x.Sun
        || e.Wind != x.Wind;
}
