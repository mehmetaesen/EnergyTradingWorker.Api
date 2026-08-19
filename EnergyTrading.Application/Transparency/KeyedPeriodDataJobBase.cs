using EnergyTrading.Domain;
using EnergyTrading.Domain.Transparency;

namespace EnergyTrading.Application.Transparency;

public abstract class KeyedPeriodDataJobBase<TEntity, TData>(
    IIntegrationJobLogService logs,
    IGenericRepository<TEntity> repository,
    IUnitOfWork unitOfWork
) : IntegrationJobBase<TData>(logs), ITransparencyReconciliationJob
    where TEntity : BaseEntity, IExternalKeyEntity, new()
{
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
        var database = existing.ToDictionary(entity => (entity.Date, entity.TimeOfPeriodId, entity.ExternalKey));

        var missing = unique.Where(item => !database.ContainsKey(GetKey(item))).Select(item => FormatKey(GetKey(item))).ToList();
        var different = unique.Where(item => database.TryGetValue(GetKey(item), out var entity) &&
            TransparencyValue.HasChanges(CreateCandidate(item), entity)).Select(item => FormatKey(GetKey(item))).ToList();
        var extra = database.Keys.Where(key => !keys.Contains(key)).Select(FormatKey).ToList();

        return new ReconciliationResult(
            JobCode, databaseStart, databaseEnd, data.Count, unique.Count, existing.Count,
            missing.Count, different.Count, extra.Count,
            missing.Take(20).ToList(), different.Take(20).ToList(), extra.Take(20).ToList());
    }

    private static string FormatKey((DateOnly Date, int Period, string ExternalKey) key) =>
        $"{key.Date:yyyy-MM-dd}/{key.Period}/{key.ExternalKey}";

    private TEntity CreateCandidate(TData item)
    {
        var key = GetKey(item);
        var candidate = new TEntity
        {
            Date = key.Date,
            TimeOfPeriodId = key.Period,
            ExternalKey = key.ExternalKey,
        };
        Map(item, candidate);
        return candidate;
    }

    protected abstract (DateOnly Date, int Period, string ExternalKey) GetKey(TData item);
    protected abstract void Map(TData source, TEntity target);

    protected override async Task<SaveResult> SaveAsync(
        IReadOnlyList<TData> data,
        CancellationToken ct
    )
    {
        var unique = data.GroupBy(GetKey).Select(group => group.Last()).ToList();
        return await unitOfWork.ExecuteInTransactionAsync(
            async transactionCt =>
            {
                var inserts = new List<TEntity>();
                var updates = new List<TEntity>();
                foreach (var dayGroup in unique.GroupBy(item => GetKey(item).Date))
                {
                    var periods = dayGroup.Select(item => GetKey(item).Period).Distinct().ToArray();
                    var existing = await repository.GetListAsync(
                        dayGroup.Key,
                        periods,
                        transactionCt
                    );
                    var index = existing.ToDictionary(entity =>
                        (entity.Date, entity.TimeOfPeriodId, entity.ExternalKey)
                    );
                    foreach (var item in dayGroup)
                    {
                        var key = GetKey(item);
                        if (!index.TryGetValue(key, out var entity))
                        {
                            entity = new TEntity
                            {
                                Date = key.Date,
                                TimeOfPeriodId = key.Period,
                                ExternalKey = key.ExternalKey,
                            };
                            Map(item, entity);
                            inserts.Add(entity);
                        }
                        else if (TransparencyValue.HasChanges(CreateCandidate(item), entity))
                        {
                            Map(item, entity);
                            updates.Add(entity);
                        }
                    }
                }
                await repository.InsertAsync(inserts, transactionCt);
                await repository.UpdateAsync(updates, transactionCt);
                return new SaveResult(inserts.Count, updates.Count);
            },
            ct
        );
    }
}
