using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public sealed class WindGenerationAndForecast : BaseEntity, IQuarterPeriodEntity
{
    public DateOnly Date { get; set; }
    public int TimeOfPeriodId { get; set; }
    public int Quarter { get; set; }
    public TimeOnly Hour { get; set; }
    public decimal? Forecast { get; set; }
    public decimal? Generation { get; set; }
    public decimal? Quantile5 { get; set; }
    public decimal? Quantile25 { get; set; }
    public decimal? Quantile75 { get; set; }
    public decimal? Quantile95 { get; set; }
}
