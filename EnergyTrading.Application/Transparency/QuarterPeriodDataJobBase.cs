using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public abstract class QuarterPeriodDataJobBase<TEntity, TData>(
    IIntegrationJobLogService logService,
    IGenericRepository<TEntity> repository,
    IUnitOfWork unitOfWork
) : IntegrationJobBase<TData>(logService), ITransparencyReconciliationJob
    where TEntity : BaseEntity, IQuarterPeriodEntity, new()
{
    protected abstract (DateOnly Date, int Period, int Quarter) GetKey(TData item);
    protected abstract void Map(TData source, TEntity target);

    public async Task<ReconciliationResult> ReconcileAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var data = await FetchAsync(startDate, endDate, cancellationToken);
        var unique = data.GroupBy(GetKey).Select(group => group.Last()).ToList();
        var keys = unique.Select(GetKey).ToHashSet();
        var databaseStart = keys.Count == 0 ? startDate : keys.Min(key => key.Date);
        var databaseEnd = keys.Count == 0 ? endDate : keys.Max(key => key.Date);
        var existing = await repository.GetDateRangeAsync(databaseStart, databaseEnd, cancellationToken);
        var database = existing.ToDictionary(entity =>
            (entity.Date, entity.TimeOfPeriodId, entity.Quarter));

        var missing = unique
            .Where(item => !database.ContainsKey(GetKey(item)))
            .Select(item => FormatKey(GetKey(item)))
            .ToList();
        var different = unique
            .Where(item => database.TryGetValue(GetKey(item), out var entity)
                           && TransparencyValue.HasChanges(CreateCandidate(item), entity))
            .Select(item => FormatKey(GetKey(item)))
            .ToList();
        var extra = database.Keys.Where(key => !keys.Contains(key)).Select(FormatKey).ToList();

        return new ReconciliationResult(
            JobCode, databaseStart, databaseEnd, data.Count, unique.Count, existing.Count,
            missing.Count, different.Count, extra.Count,
            missing.Take(20).ToList(), different.Take(20).ToList(), extra.Take(20).ToList());
    }

    protected override async Task<SaveResult> SaveAsync(
        IReadOnlyList<TData> data,
        CancellationToken cancellationToken)
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
                    var periods = dayGroup.Select(item => GetKey(item).Period).Distinct().ToArray();
                    var existing = await repository.GetListAsync(dayGroup.Key, periods, ct);
                    var index = existing.ToDictionary(entity =>
                        (entity.Date, entity.TimeOfPeriodId, entity.Quarter));

                    foreach (var item in dayGroup)
                    {
                        var key = GetKey(item);
                        if (!index.TryGetValue(key, out var entity))
                        {
                            entity = CreateCandidate(item);
                            inserts.Add(entity);
                        }
                        else if (TransparencyValue.HasChanges(CreateCandidate(item), entity))
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
            cancellationToken);
    }

    private TEntity CreateCandidate(TData item)
    {
        var key = GetKey(item);
        TransparencyPeriod.Validate(key.Period);
        if (key.Quarter is < 1 or > 4)
            throw new InvalidOperationException($"Quarter must be between 1 and 4, but was {key.Quarter}.");

        var candidate = new TEntity
        {
            Date = key.Date,
            TimeOfPeriodId = key.Period,
            Quarter = key.Quarter,
        };
        Map(item, candidate);
        return candidate;
    }

    private static string FormatKey((DateOnly Date, int Period, int Quarter) key) =>
        $"{key.Date:yyyy-MM-dd}/{key.Period}/Q{key.Quarter}";
}
