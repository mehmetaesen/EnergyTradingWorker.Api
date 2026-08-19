using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class SecondaryFrequencyCapacityPriceJob(
    IIntegrationJobLogService logs,
    ITransparencyApiClient client,
    IGenericRepository<SecondaryFrequencyCapacityPrice> repo,
    IUnitOfWork uow,
    ITurkeyClock clock
) : PeriodDataJobBase<SecondaryFrequencyCapacityPrice, FrequencyCapacityPriceItem>(logs, repo, uow)
{
    public const string Code = "TRANSPARENCY_SFK_PRICE";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (clock.Today, clock.Today);

    protected override async Task<IReadOnlyList<FrequencyCapacityPriceItem>> FetchAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken ct
    )
    {
        var r = TransparencyPeriod.Range(start, end);
        return (
            await client.GetSecondaryFrequencyCapacityPriceAsync(new(r.Start, r.End), ct)
        ).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(FrequencyCapacityPriceItem x) =>
        (DateOnly.FromDateTime(x.Date.DateTime), TransparencyPeriod.Hour(x.Hour));

    protected override void Map(FrequencyCapacityPriceItem x, SecondaryFrequencyCapacityPrice e) =>
        e.Price = x.Price;

}
