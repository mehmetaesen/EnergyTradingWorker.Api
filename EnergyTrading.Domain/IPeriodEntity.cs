namespace EnergyTrading.Domain;

public interface IPeriodEntity
{
    DateOnly Date { get; set; }
    int TimeOfPeriodId { get; set; }
}
