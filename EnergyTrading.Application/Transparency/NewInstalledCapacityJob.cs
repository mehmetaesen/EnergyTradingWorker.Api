using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class NewInstalledCapacityJob(
    IIntegrationJobLogService l,
    ITransparencyApiClient c,
    IGenericRepository<NewInstalledCapacity> r,
    IUnitOfWork u,
    ITurkeyClock k,
    ITransparencyRegionProvider p
) : KeyedPeriodDataJobBase<NewInstalledCapacity, InstalledCapacityItem>(l, r, u)
{
    public const string Code = "TRANSPARENCY_NEW_INSTALLED_CAPACITY";
    private DateOnly requestDate;
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (k.Today, k.Today);

    protected override async Task<IReadOnlyList<InstalledCapacityItem>> FetchAsync(
        DateOnly s,
        DateOnly e,
        CancellationToken ct
    )
    {
        requestDate = s;
        return (
            await c.GetDataAsync<InstalledCapacityResponse>(
                "v1/renewables/data/new-installed-capacity",
                new
                {
                    period = new DateTimeOffset(
                        s.ToDateTime(TimeOnly.MinValue),
                        TransparencyPeriod.TurkeyOffset
                    ),
                },
                ct
            )
        ).InstalledCapacities;
    }

    protected override (DateOnly Date, int Period, string ExternalKey) GetKey(
        InstalledCapacityItem x
    ) => (requestDate, 1, x.RenewableEnergyType);

    protected override void Map(InstalledCapacityItem x, NewInstalledCapacity y) =>
        (y.RenewableEnergyType, y.LicensedCapacity, y.UnlicensedCapacity, y.Total) = (
            x.RenewableEnergyType,
            x.LicencedCapacity,
            x.UnlicencedCapacity,
            x.Total
        );

    protected override bool HasChanges(InstalledCapacityItem x, NewInstalledCapacity y) =>
        x.RenewableEnergyType != y.RenewableEnergyType
        || x.LicencedCapacity != y.LicensedCapacity
        || x.UnlicencedCapacity != y.UnlicensedCapacity
        || x.Total != y.Total;
}
