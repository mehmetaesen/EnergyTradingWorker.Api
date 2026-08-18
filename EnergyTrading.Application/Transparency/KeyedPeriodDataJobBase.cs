using EnergyTrading.Domain;
using EnergyTrading.Domain.Transparency;

namespace EnergyTrading.Application.Transparency;

public abstract class KeyedPeriodDataJobBase<TEntity, TData>(
    IIntegrationJobLogService logs,
    IGenericRepository<TEntity> repository,
    IUnitOfWork unitOfWork
) : IntegrationJobBase<TData>(logs)
    where TEntity : BaseEntity, IExternalKeyEntity, new()
{
    protected abstract (DateOnly Date, int Period, string ExternalKey) GetKey(TData item);
    protected abstract void Map(TData source, TEntity target);
    protected abstract bool HasChanges(TData source, TEntity target);

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
                        else if (HasChanges(item, entity))
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
