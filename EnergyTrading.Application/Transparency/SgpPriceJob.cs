using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class SgpPriceJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<SgpPrice> r,
    IUnitOfWork u,
    ITurkeyClock k,
    ITransparencyRegionProvider p
) : RawTransparencyJobBase<SgpPrice>(l, c, r, u, k, p)
{
    public const string Code = "TRANSPARENCY_SGP_PRICE";
    protected override string JobCode => Code;
    protected override string Path => "/natural-gas-service/v1/markets/sgp/data/sgp-price";

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (Clock.Today, Clock.Today);
}
