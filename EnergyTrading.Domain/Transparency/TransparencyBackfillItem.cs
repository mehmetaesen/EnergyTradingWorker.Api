namespace EnergyTrading.Domain.Transparency;

public sealed class TransparencyBackfillItem : BaseEntity
{
    public long BackfillRunId { get; set; }
    public TransparencyBackfillRun BackfillRun { get; set; } = null!;
    public string HangfireJobId { get; set; } = string.Empty;
    public string JobCode { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
