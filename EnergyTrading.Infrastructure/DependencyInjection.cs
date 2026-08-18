using EnergyTrading.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnergyTrading.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddEnergyTradingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("EnergyTrading") ?? throw new InvalidOperationException("ConnectionStrings:EnergyTrading is required.");
        services.AddDbContextFactory<EnergyTradingDbContext>(o => o.UseNpgsql(connection));
        services.AddOptions<TransparencyOptions>().Bind(configuration.GetSection(TransparencyOptions.SectionName)).ValidateOnStart(); services.AddSingleton(TimeProvider.System); services.AddSingleton<ITurkeyClock, TurkeyClock>();
        services.AddSingleton<ITransparencyRegionProvider, TransparencyRegionProvider>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>)); services.AddScoped<IUnitOfWork, EfUnitOfWork>(); services.AddScoped<IIntegrationJobLogService, IntegrationJobLogService>();
        services.AddScoped<MarketClearingPriceJob>(); services.AddScoped<SystemMarginalPriceJob>();
        services.AddScoped<LoadEstimationPlanJob>(); services.AddScoped<RealTimeConsumptionJob>();
        services.AddScoped<GenerationPlanJob>(); services.AddScoped<FirstVersionGenerationPlanJob>();
        services.AddScoped<InjectionQuantityJob>(); services.AddScoped<PrimaryFrequencyCapacityPriceJob>();
        services.AddScoped<SecondaryFrequencyCapacityPriceJob>(); services.AddScoped<SystemDirectionJob>();
        services.AddScoped<WindGenerationForecastJob>();
        services.AddScoped<FinalGenerationPlanJob>(); services.AddScoped<NewInstalledCapacityJob>();
        services.AddScoped<PlannedPowerOutageJob>(); services.AddScoped<UnplannedPowerOutageJob>();
        services.AddScoped<SgpPriceJob>(); services.AddScoped<AvailableInstalledCapacityJob>();
        services.AddScoped<UnlicensedGenerationJob>(); services.AddScoped<RealTimeGenerationJob>();
        services.AddScoped<OrderSummaryUpJob>(); services.AddScoped<OrderSummaryDownJob>();
        services.AddScoped<ClearingQuantityJob>(); services.AddScoped<IdmWeightedAveragePriceJob>();
        services.AddScoped<IdmMatchingQuantityJob>(); services.AddScoped<WithdrawalQuantityJob>();
        services.AddScoped<IdmContractSummaryJob>();
        services.AddHttpClient<ITransparencyAuthenticationClient, TransparencyAuthenticationClient>((sp, client) => client.Timeout = TimeSpan.FromSeconds(sp.GetRequiredService<IOptions<TransparencyOptions>>().Value.TimeoutSeconds)).AddStandardResilienceHandler(o => o.Retry.MaxRetryAttempts = 3);
        services.AddHttpClient<ITransparencyHttpClient, TransparencyHttpClient>((sp, client) => { var o = sp.GetRequiredService<IOptions<TransparencyOptions>>().Value; client.BaseAddress = new Uri(o.BaseUrl); client.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds); }).AddStandardResilienceHandler(o => o.Retry.MaxRetryAttempts = 3);
        services.AddScoped<ITransparencyApiClient, TransparencyApiClient>();
        return services;
    }
}

public sealed class TransparencyRegionProvider(IOptions<TransparencyOptions> options) : ITransparencyRegionProvider
{
    public string SystemMarginalPriceRegion => options.Value.SystemMarginalPriceRegion;
}
