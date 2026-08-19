using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class MarketClearingPriceJob(
    IIntegrationJobLogService logService,
    ITransparencyApiClient client,
    IGenericRepository<MarketClearingPrice> repository,
    IUnitOfWork unitOfWork,
    ITurkeyClock clock
) : PeriodDataJobBase<MarketClearingPrice, MarketClearingPriceDto>(logService, repository, unitOfWork)
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

    protected override (DateOnly Date, int Period) GetKey(MarketClearingPriceDto item) =>
        (item.Date, item.TimeOfPeriodId);

    protected override void Map(MarketClearingPriceDto source, MarketClearingPrice target) =>
        (target.Price, target.PriceUsd, target.PriceEur) =
        (source.Price, source.PriceUsd, source.PriceEur);

    private static int ParsePeriod(string hour)
    {
        var first = hour.Split('-', StringSplitOptions.TrimEntries)[0];
        if (!TimeOnly.TryParse(first, out var time))
            throw new FormatException("EPİAŞ returned an invalid hour value.");
        return time.Hour + 1;
    }
}
