using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public abstract class PeriodDataJobBase<TEntity, TData>(
    IIntegrationJobLogService logService,
    IGenericRepository<TEntity> repository,
    IUnitOfWork unitOfWork
) : IntegrationJobBase<TData>(logService)
    where TEntity : BaseEntity, IPeriodEntity, new()
{
    protected abstract (DateOnly Date, int Period) GetKey(TData item);
    protected abstract void Map(TData source, TEntity target);
    protected abstract bool HasChanges(TData source, TEntity target);

    protected override async Task<SaveResult> SaveAsync(
        IReadOnlyList<TData> data,
        CancellationToken cancellationToken
    )
    {
        if (data.Count == 0)
            return new SaveResult(0, 0);
        var unique = data.GroupBy(GetKey).Select(group => group.Last()).ToList();
        return await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var inserts = new List<TEntity>();
                var updates = new List<TEntity>();
                foreach (var dayGroup in unique.GroupBy(item => GetKey(item).Date))
                {
                    var periods = dayGroup.Select(item => GetKey(item).Period).ToArray();
                    var existing = await repository.GetListAsync(dayGroup.Key, periods, ct);
                    var index = existing.ToDictionary(entity =>
                        (entity.Date, entity.TimeOfPeriodId)
                    );
                    foreach (var item in dayGroup)
                    {
                        var key = GetKey(item);
                        if (!index.TryGetValue(key, out var entity))
                        {
                            entity = new TEntity { Date = key.Date, TimeOfPeriodId = key.Period };
                            Map(item, entity);
                            inserts.Add(entity);
                        }
                        else if (HasChanges(item, entity))
                        {
                            Map(item, entity);
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

internal static class TransparencyPeriod
{
    internal static readonly TimeSpan TurkeyOffset = TimeSpan.FromHours(3);

    internal static (DateTimeOffset Start, DateTimeOffset End) Range(
        DateOnly startDate,
        DateOnly endDate
    ) =>
        (
            new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TurkeyOffset),
            new DateTimeOffset(endDate.ToDateTime(new TimeOnly(23, 59, 59)), TurkeyOffset)
        );

    internal static int Hour(string value)
    {
        var first = value.Split('-', StringSplitOptions.TrimEntries)[0];
        if (!TimeOnly.TryParse(first, out var time))
            throw new FormatException($"EPİAŞ returned an invalid time value: '{value}'.");
        return time.Hour + 1;
    }

    internal static int Hour(int value) => value is >= 1 and <= 24 ? value : value + 1;

    internal static int TenMinute(DateTimeOffset value) => value.Hour * 6 + value.Minute / 10 + 1;
}
