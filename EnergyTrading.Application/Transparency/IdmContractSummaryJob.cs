using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class IdmContractSummaryJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<IdmContractSummary> r,
    IUnitOfWork u,
    ITurkeyClock k,
    ITransparencyRegionProvider p
) : RawTransparencyJobBase<IdmContractSummary>(l, c, r, u, k, p)
{
    public const string Code = "TRANSPARENCY_IDM_CONTRACT_SUMMARY";
    protected override string JobCode => Code;
    protected override string Path => "/reporting-service/v1/data/idm-contract-summary";

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (Clock.Today, Clock.Today);
}
