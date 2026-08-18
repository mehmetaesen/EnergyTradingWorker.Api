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
) : IntegrationJobBase<SystemMarginalPriceDto>(logService)
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

    protected override async Task<SaveResult> SaveAsync(
        IReadOnlyList<SystemMarginalPriceDto> data,
        CancellationToken cancellationToken
    )
    {
        if (data.Count == 0)
            return new SaveResult(0, 0);
        var unique = data.GroupBy(item => (item.Date, item.TimeOfPeriodId))
            .Select(group => group.Last())
            .ToList();

        return await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var inserts = new List<SystemMarginalPrice>();
                var updates = new List<SystemMarginalPrice>();
                foreach (var dayGroup in unique.GroupBy(item => item.Date))
                {
                    var existing = await repository.GetListAsync(
                        dayGroup.Key,
                        dayGroup.Select(item => item.TimeOfPeriodId).ToArray(),
                        ct
                    );
                    var index = existing.ToDictionary(item => (item.Date, item.TimeOfPeriodId));
                    foreach (var item in dayGroup)
                    {
                        if (!index.TryGetValue((item.Date, item.TimeOfPeriodId), out var entity))
                            inserts.Add(
                                new SystemMarginalPrice
                                {
                                    Date = item.Date,
                                    TimeOfPeriodId = item.TimeOfPeriodId,
                                    Price = item.Price,
                                }
                            );
                        else if (entity.Price != item.Price)
                        {
                            entity.Price = item.Price;
                            updates.Add(entity);
                        }
                    }
                }

                await repository.InsertAsync(inserts, ct);
                await repository.UpdateAsync(updates, ct);
                return new SaveResult(inserts.Count, updates.Count);
            },
            cancellationToken
        );
    }
}
