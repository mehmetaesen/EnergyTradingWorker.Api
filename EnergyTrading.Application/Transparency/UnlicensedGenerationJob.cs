using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class UnlicensedGenerationJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<UnlicensedGenerationAmount> r,
    IUnitOfWork u,
    ITurkeyClock k,
    ITransparencyRegionProvider p
) : PeriodDataJobBase<UnlicensedGenerationAmount, UnlicensedGenerationItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_UNLICENSED_GENERATION";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today.AddDays(-1), k.Today.AddDays(-1));

    protected override async Task<IReadOnlyList<UnlicensedGenerationItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        var x = TransparencyPeriod.Range(s, e);
        return (
            await c.GetDataAsync<UnlicensedGenerationResponse>(
                "v1/renewables/data/unlicensed-generation-amount",
                new SystemDirectionRequest(x.Start, x.End, p.SystemMarginalPriceRegion),
                ct
            )
        ).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(UnlicensedGenerationItem x) =>
        (DateOnly.FromDateTime(x.Date.Date), TransparencyPeriod.Hour(x.Time));

    protected override void Map(UnlicensedGenerationItem x, UnlicensedGenerationAmount y) =>
        (y.Biogas, y.Biomass, y.Other, y.Solar, y.ChannelType, y.Wind, y.Total) = (
            x.Biyogaz,
            x.Biokutle,
            x.Diger,
            x.Gunes,
            x.KanalTipi,
            x.Ruzgar,
            x.Toplam
        );

    protected override bool HasChanges(
        UnlicensedGenerationItem x,
        UnlicensedGenerationAmount y
    ) =>
        x.Biyogaz != y.Biogas
        || x.Biokutle != y.Biomass
        || x.Diger != y.Other
        || x.Gunes != y.Solar
        || x.KanalTipi != y.ChannelType
        || x.Ruzgar != y.Wind
        || x.Toplam != y.Total;
}
