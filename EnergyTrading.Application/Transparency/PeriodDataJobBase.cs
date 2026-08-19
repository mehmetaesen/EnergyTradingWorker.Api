using System.Globalization;
using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public abstract class PeriodDataJobBase<TEntity, TData>(
    IIntegrationJobLogService logService,
    IGenericRepository<TEntity> repository,
    IUnitOfWork unitOfWork
) : IntegrationJobBase<TData>(logService), ITransparencyReconciliationJob
    where TEntity : BaseEntity, IPeriodEntity, new()
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
        var database = existing.ToDictionary(entity => (entity.Date, entity.TimeOfPeriodId));

        var missing = unique.Where(item => !database.ContainsKey(GetKey(item))).Select(item => FormatKey(GetKey(item))).ToList();
        var different = unique.Where(item => database.TryGetValue(GetKey(item), out var entity) &&
            TransparencyValue.HasChanges(CreateCandidate(item), entity)).Select(item => FormatKey(GetKey(item))).ToList();
        var extra = database.Keys.Where(key => !keys.Contains(key)).Select(FormatKey).ToList();

        return new ReconciliationResult(
            JobCode, databaseStart, databaseEnd, data.Count, unique.Count, existing.Count,
            missing.Count, different.Count, extra.Count,
            missing.Take(20).ToList(), different.Take(20).ToList(), extra.Take(20).ToList());
    }

    private static string FormatKey((DateOnly Date, int Period) key) => $"{key.Date:yyyy-MM-dd}/{key.Period}";

    private TEntity CreateCandidate(TData item)
    {
        var key = GetKey(item);
        TransparencyPeriod.Validate(key.Period);
        var candidate = new TEntity { Date = key.Date, TimeOfPeriodId = key.Period };
        Map(item, candidate);
        return candidate;
    }

    protected abstract (DateOnly Date, int Period) GetKey(TData item);
    protected abstract void Map(TData source, TEntity target);

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
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var dateTime))
            return dateTime.Hour + 1;

        var first = value.Split('-', StringSplitOptions.TrimEntries)[0];
        if (!TimeOnly.TryParse(first, out var time))
            throw new FormatException($"EPİAŞ returned an invalid time value: '{value}'.");
        return time.Hour + 1;
    }

    internal static int Hour(int value) => value is >= 1 and <= 24 ? value : value + 1;

    internal static void Validate(int value)
    {
        if (value is < 1 or > 24)
            throw new InvalidOperationException(
                $"TimeOfPeriodId must be between 1 and 24, but was {value}.");
    }

}

internal static class TransparencyValue
{
    private static readonly HashSet<string> AuditProperties =
        [nameof(BaseEntity.Id), nameof(BaseEntity.CreatedDate), nameof(BaseEntity.UpdatedDate)];

    internal static bool HasChanges<TEntity>(TEntity expected, TEntity actual)
    {
        foreach (var property in typeof(TEntity).GetProperties().Where(property =>
                     property.CanRead && !AuditProperties.Contains(property.Name)))
        {
            var expectedValue = property.GetValue(expected);
            var actualValue = property.GetValue(actual);
            if (expectedValue is decimal expectedDecimal && actualValue is decimal actualDecimal)
            {
                if (Different(expectedDecimal, actualDecimal))
                    return true;
                continue;
            }

            if (!Equals(expectedValue, actualValue))
                return true;
        }

        return false;
    }

    internal static bool Different(decimal expected, decimal actual) =>
        Normalize(expected) != Normalize(actual);

    private static decimal Normalize(decimal value) =>
        decimal.Round(value, 6, MidpointRounding.AwayFromZero);
}
