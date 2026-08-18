using EnergyTrading.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnergyTrading.Infrastructure;

public sealed class EnergyTradingDbContext(DbContextOptions<EnergyTradingDbContext> options) : DbContext(options)
{
    public DbSet<IntegrationJob> IntegrationJobs => Set<IntegrationJob>();
    public DbSet<IntegrationJobLog> IntegrationJobLogs => Set<IntegrationJobLog>();
    public DbSet<MarketClearingPrice> MarketClearingPrices => Set<MarketClearingPrice>();
    public DbSet<SystemMarginalPrice> SystemMarginalPrices => Set<SystemMarginalPrice>();
    public DbSet<LoadEstimationPlan> LoadEstimationPlans => Set<LoadEstimationPlan>();
    public DbSet<RealTimeConsumption> RealTimeConsumptions => Set<RealTimeConsumption>();
    public DbSet<GenerationPlan> GenerationPlans => Set<GenerationPlan>();
    public DbSet<FirstVersionGenerationPlan> FirstVersionGenerationPlans => Set<FirstVersionGenerationPlan>();
    public DbSet<InjectionQuantity> InjectionQuantities => Set<InjectionQuantity>();
    public DbSet<PrimaryFrequencyCapacityPrice> PrimaryFrequencyCapacityPrices => Set<PrimaryFrequencyCapacityPrice>();
    public DbSet<SecondaryFrequencyCapacityPrice> SecondaryFrequencyCapacityPrices => Set<SecondaryFrequencyCapacityPrice>();
    public DbSet<SystemDirection> SystemDirections => Set<SystemDirection>();
    public DbSet<WindGenerationAndForecast> WindGenerationAndForecasts => Set<WindGenerationAndForecast>();
    public DbSet<FinalGenerationPlan> FinalGenerationPlans => Set<FinalGenerationPlan>();
    public DbSet<NewInstalledCapacity> NewInstalledCapacities => Set<NewInstalledCapacity>();
    public DbSet<PlannedPowerOutage> PlannedPowerOutages => Set<PlannedPowerOutage>();
    public DbSet<UnplannedPowerOutage> UnplannedPowerOutages => Set<UnplannedPowerOutage>();
    public DbSet<SgpPrice> SgpPrices => Set<SgpPrice>();
    public DbSet<AvailableInstalledCapacity> AvailableInstalledCapacities => Set<AvailableInstalledCapacity>();
    public DbSet<UnlicensedGenerationAmount> UnlicensedGenerationAmounts => Set<UnlicensedGenerationAmount>();
    public DbSet<RealTimeGeneration> RealTimeGenerations => Set<RealTimeGeneration>();
    public DbSet<UpRegulationOrderSummary> UpRegulationOrderSummaries => Set<UpRegulationOrderSummary>();
    public DbSet<DownRegulationOrderSummary> DownRegulationOrderSummaries => Set<DownRegulationOrderSummary>();
    public DbSet<ClearingQuantity> ClearingQuantities => Set<ClearingQuantity>();
    public DbSet<IdmWeightedAveragePrice> IdmWeightedAveragePrices => Set<IdmWeightedAveragePrice>();
    public DbSet<IdmMatchingQuantity> IdmMatchingQuantities => Set<IdmMatchingQuantity>();
    public DbSet<WithdrawalQuantity> WithdrawalQuantities => Set<WithdrawalQuantity>();
    public DbSet<IdmContractSummary> IdmContractSummaries => Set<IdmContractSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnergyTradingDbContext).Assembly);
        modelBuilder.UsePostgreSqlSnakeCaseNames();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedDate = now;
            if (entry.State == EntityState.Modified) entry.Entity.UpdatedDate = now;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
