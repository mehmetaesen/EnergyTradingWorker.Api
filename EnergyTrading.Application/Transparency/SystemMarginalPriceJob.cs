using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class SystemMarginalPriceJob(
    IIntegrationJobLogService logService,
    ITransparencyApiClient client,
    IGenericRepository<SystemMarginalPrice> repository,
    IUnitOfWork unitOfWork,
    ITurkeyClock clock,
    ITransparencyRegionProvider regionProvider
) : PeriodDataJobBase<SystemMarginalPrice, SystemMarginalPriceDto>(logService, repository, unitOfWork)
{
    public const string Code = "TRANSPARENCY_SMF";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (clock.Today, clock.Today);

    protected override async Task<IReadOnlyList<SystemMarginalPriceDto>> FetchAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken
    )
    {
        if (startDate > clock.Today || endDate > clock.Today)
            throw new ArgumentOutOfRangeException(
                nameof(endDate),
                "System marginal prices cannot be requested for a future date."
            );

        var offset = TimeSpan.FromHours(3);
        var start = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), offset);
        var safeNow = clock.Now.AddMinutes(-1);
        var end =
            endDate == clock.Today
                ? new DateTimeOffset(
                    safeNow.Year,
                    safeNow.Month,
                    safeNow.Day,
                    safeNow.Hour,
                    safeNow.Minute,
                    safeNow.Second,
                    safeNow.Offset
                )
                : new DateTimeOffset(endDate.AddDays(1).ToDateTime(TimeOnly.MinValue), offset);
        var response = await client.GetSystemMarginalPriceAsync(
            new SystemMarginalPriceRequest(start, end, regionProvider.SystemMarginalPriceRegion),
            cancellationToken
        );

        return response
            .Items.Select(item => new SystemMarginalPriceDto(
                DateOnly.FromDateTime(item.Date.DateTime),
                item.Hour.Hour + 1,
                item.SystemMarginalPrice
            ))
            .ToList();
    }

    protected override (DateOnly Date, int Period) GetKey(SystemMarginalPriceDto item) =>
        (item.Date, item.TimeOfPeriodId);

    protected override void Map(SystemMarginalPriceDto source, SystemMarginalPrice target) =>
        target.Price = source.Price;
}
