using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed class WindGenerationForecastJob(
    IIntegrationJobLogService logs,
    ITransparencyApiClient client,
    IGenericRepository<WindGenerationAndForecast> repo,
    IUnitOfWork uow,
    ITurkeyClock clock
) : PeriodDataJobBase<WindGenerationAndForecast, WindGenerationForecastItem>(logs, repo, uow)
{
    public const string Code = "TRANSPARENCY_RES_GENERATION_FORECAST";
    protected override string JobCode => Code;

    protected override (DateOnly StartDate, DateOnly EndDate) GetDefaultDateRange() =>
        (clock.Today, clock.Today);

    protected override async Task<IReadOnlyList<WindGenerationForecastItem>> FetchAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken ct
    )
    {
        var r = TransparencyPeriod.Range(start, end);
        return (await client.GetWindGenerationForecastAsync(new(r.Start, r.End), ct)).Items;
    }

    protected override (DateOnly Date, int Period) GetKey(WindGenerationForecastItem x) =>
        (DateOnly.FromDateTime(x.Date.DateTime), TransparencyPeriod.TenMinute(x.Time));

    protected override void Map(WindGenerationForecastItem x, WindGenerationAndForecast e)
    {
        e.Forecast = x.Forecast;
        e.Generation = x.Generation;
        e.Quantile5 = x.Quarter1;
        e.Quantile25 = x.Quarter2;
        e.Quantile75 = x.Quarter3;
        e.Quantile95 = x.Quarter4;
    }

    protected override bool HasChanges(WindGenerationForecastItem x, WindGenerationAndForecast e) =>
        e.Forecast != x.Forecast
        || e.Generation != x.Generation
        || e.Quantile5 != x.Quarter1
        || e.Quantile25 != x.Quarter2
        || e.Quantile75 != x.Quarter3
        || e.Quantile95 != x.Quarter4;
}
