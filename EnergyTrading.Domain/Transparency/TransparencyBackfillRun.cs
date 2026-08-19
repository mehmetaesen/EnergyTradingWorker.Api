namespace EnergyTrading.Domain.Transparency;

public sealed class TransparencyBackfillRun : BaseEntity
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalJobCount { get; set; }
    public DateTimeOffset StartedDate { get; set; }
    public ICollection<TransparencyBackfillItem> Items { get; set; } = [];
}
