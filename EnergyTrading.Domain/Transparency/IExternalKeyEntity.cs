namespace EnergyTrading.Domain.Transparency;

public interface IExternalKeyEntity : IPeriodEntity
{
    string ExternalKey { get; set; }
}
