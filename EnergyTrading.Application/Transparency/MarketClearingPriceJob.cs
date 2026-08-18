using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class MarketClearingPriceJob(
    IIntegrationJobLogService logService,
    ITransparencyApiClient client,
    IGenericRepository<MarketClearingPrice> repository,
    IUnitOfWork unitOfWork,
    ITurkeyClock clock
) : IntegrationJobBase<MarketClearingPriceDto>(logService)
{
    public const string Code = "TRANSPARENCY_PTF";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange()
    {
        var day = clock.Today.AddDays(1);
        return (day, day);
    }

    protected override async Task<IReadOnlyList<MarketClearingPriceDto>> FetchAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken
    )
    {
        var offset = TimeSpan.FromHours(3);
        var start = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), offset);
        var end = new DateTimeOffset(endDate.ToDateTime(new TimeOnly(23, 59, 59)), offset);
        var response = await client.GetMcpAsync(new McpRequest(start, end), cancellationToken);
        return response
            .Items.Select(x => new MarketClearingPriceDto(
                DateOnly.FromDateTime(x.Date.DateTime),
                ParsePeriod(x.Hour),
                x.Price,
                x.PriceUsd,
                x.PriceEur
            ))
            .ToList();
    }

    protected override async Task<SaveResult> SaveAsync(
        IReadOnlyList<MarketClearingPriceDto> data,
        CancellationToken cancellationToken
    )
    {
        if (data.Count == 0)
            return new SaveResult(0, 0);
        var unique = data.GroupBy(x => (x.Date, x.TimeOfPeriodId)).Select(x => x.Last()).ToList();
        return await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var inserts = new List<MarketClearingPrice>();
                var updates = new List<MarketClearingPrice>();
                foreach (var dayGroup in unique.GroupBy(x => x.Date))
                {
                    var existing = await repository.GetListAsync(
                        dayGroup.Key,
                        dayGroup.Select(x => x.TimeOfPeriodId).ToArray(),
                        ct
                    );
                    var index = existing.ToDictionary(x => (x.Date, x.TimeOfPeriodId));
                    foreach (var item in dayGroup)
                    {
                        if (!index.TryGetValue((item.Date, item.TimeOfPeriodId), out var entity))
                            inserts.Add(
                                new MarketClearingPrice
                                {
                                    Date = item.Date,
                                    TimeOfPeriodId = item.TimeOfPeriodId,
                                    Price = item.Price,
                                    PriceUsd = item.PriceUsd,
                                    PriceEur = item.PriceEur,
                                }
                            );
                        else if (
                            entity.Price != item.Price
                            || entity.PriceUsd != item.PriceUsd
                            || entity.PriceEur != item.PriceEur
                        )
                        {
                            entity.Price = item.Price;
                            entity.PriceUsd = item.PriceUsd;
                            entity.PriceEur = item.PriceEur;
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

    private static int ParsePeriod(string hour)
    {
        var first = hour.Split('-', StringSplitOptions.TrimEntries)[0];
        if (!TimeOnly.TryParse(first, out var time))
            throw new FormatException("EPİAŞ returned an invalid hour value.");
        return time.Hour + 1;
    }
}
