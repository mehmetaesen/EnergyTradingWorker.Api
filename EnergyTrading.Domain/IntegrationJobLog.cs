namespace EnergyTrading.Domain;

public sealed class IntegrationJobLog : BaseEntity
{
    public long IntegrationJobId { get; set; }
    public IntegrationJob IntegrationJob { get; set; } = null!;
    public string? HangfireJobId { get; set; }
    public Guid CorrelationId { get; set; }
    public int? ResponseCode { get; set; }
    public string? ResponseBody { get; set; }
    public IntegrationJobStatus Status { get; set; }
    public bool IsSuccess { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorDescription { get; set; }
    public DateTimeOffset StartedDate { get; set; }
    public DateTimeOffset? CompletedDate { get; set; }
    public long? DurationMilliseconds { get; set; }
    public int FetchedRecordCount { get; set; }
    public int InsertedRecordCount { get; set; }
    public int UpdatedRecordCount { get; set; }
}
