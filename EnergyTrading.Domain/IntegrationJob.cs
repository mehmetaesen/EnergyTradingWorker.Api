namespace EnergyTrading.Domain;

public sealed class IntegrationJob : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public string QueueName { get; set; } = "default";
    public string TableName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public ICollection<IntegrationJobLog> Logs { get; set; } = [];
}
