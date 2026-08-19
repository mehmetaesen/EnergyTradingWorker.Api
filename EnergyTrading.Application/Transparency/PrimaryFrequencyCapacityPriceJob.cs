using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class PrimaryFrequencyCapacityPriceJob(
    IIntegrationJobLogService logs,
    ITransparencyApiClient client,
    IGenericRepository<PrimaryFrequencyCapacityPrice> repo,
    IUnitOfWork uow,
    ITurkeyClock clock
) : PeriodDataJobBase<PrimaryFrequencyCapacityPrice, FrequencyCapacityPriceItem>(logs, repo, uow)
{
    public const string Code = "TRANSPARENCY_PFK_PRICE";
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
        return (await client.GetPrimaryFrequencyCapacityPriceAsync(new(r.Start, r.End), ct)).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(FrequencyCapacityPriceItem x) =>
        (DateOnly.FromDateTime(x.Date.DateTime), TransparencyPeriod.Hour(x.Hour));

    protected override void Map(FrequencyCapacityPriceItem x, PrimaryFrequencyCapacityPrice e) =>
        e.Price = x.Price;

}
