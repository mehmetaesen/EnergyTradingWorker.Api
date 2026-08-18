namespace EnergyTrading.Domain;

public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? UpdatedDate { get; set; }
}
