using System.Text.Json;
using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public abstract class RawTransparencyJobBase<TEntity>(
    IIntegrationJobLogService logs,
    ITransparencyApiClient client,
    IGenericRepository<TEntity> repository,
    IUnitOfWork unitOfWork,
    ITurkeyClock clock,
    ITransparencyRegionProvider regionProvider
) : PeriodDataJobBase<TEntity, RawTransparencyData>(logs, repository, unitOfWork)
    where TEntity : BaseEntity, IRawTransparencyEntity, new()
{
    protected ITurkeyClock Clock { get; } = clock;
    protected string Region { get; } = regionProvider.SystemMarginalPriceRegion;
    protected abstract string Path { get; }

    protected virtual object CreateRequest(DateOnly start, DateOnly end)
    {
        var range = TransparencyPeriod.Range(start, end);
        return new DateRangeRequest(range.Start, range.End);
    }

    protected override async Task<IReadOnlyList<RawTransparencyData>> FetchAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken ct
    )
    {
        JsonElement response = await client.GetRawDataAsync(Path, CreateRequest(start, end), ct);
        return [new RawTransparencyData(start, response.GetRawText())];
    }

    protected override (DateOnly Date, int Period) GetKey(RawTransparencyData item) => (item.Date, 1);

    protected override void Map(RawTransparencyData source, TEntity target) =>
        target.Payload = source.Payload;

}
