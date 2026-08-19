namespace EnergyTrading.Domain;

public interface IQuarterPeriodEntity : IPeriodEntity
{
    int Quarter { get; set; }
}
