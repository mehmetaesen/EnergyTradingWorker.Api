using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class UnplannedPowerOutage : BaseEntity, IExternalKeyEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public string ExternalKey { get; set; } = string.Empty;
    public long OutageId { get; set; }
    public string DistributionCompanyName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string AffectedNeighbourhoods { get; set; } = string.Empty;
    public long AffectedSubscribers { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public decimal HourlyLoadAverage { get; set; }
}
