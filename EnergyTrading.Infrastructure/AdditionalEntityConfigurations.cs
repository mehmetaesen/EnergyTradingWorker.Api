using EnergyTrading.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnergyTrading.Infrastructure;

public abstract class PeriodEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity, IPeriodEntity
{
    protected abstract string TableName { get; }

    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.ToTable(TableName);
        builder.ConfigureBase();
        builder.Property(entity => entity.Date).HasColumnType("date");
        ConfigureIdentity(builder);
        foreach (var property in builder.Metadata.GetProperties().Where(property => property.ClrType == typeof(decimal)))
        {
            property.SetPrecision(18);
            property.SetScale(6);
        }
    }

    protected virtual void ConfigureIdentity(EntityTypeBuilder<TEntity> builder) =>
        builder.HasIndex(entity => new { entity.Date, entity.TimeOfPeriodId }).IsUnique();
}

public abstract class KeyedPeriodEntityConfiguration<TEntity> : PeriodEntityConfiguration<TEntity>
    where TEntity : BaseEntity, IExternalKeyEntity
{
    protected override void ConfigureIdentity(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(entity => entity.ExternalKey).HasMaxLength(300).IsRequired();
        builder.HasIndex(entity => new { entity.Date, entity.TimeOfPeriodId, entity.ExternalKey }).IsUnique();
    }
}

public sealed class LoadEstimationPlanConfiguration : PeriodEntityConfiguration<LoadEstimationPlan> { protected override string TableName => "load_estimation_plans"; }
public sealed class RealTimeConsumptionConfiguration : PeriodEntityConfiguration<RealTimeConsumption> { protected override string TableName => "real_time_consumptions"; }
public sealed class GenerationPlanConfiguration : PeriodEntityConfiguration<GenerationPlan> { protected override string TableName => "generation_plans"; }
public sealed class FirstVersionGenerationPlanConfiguration : PeriodEntityConfiguration<FirstVersionGenerationPlan> { protected override string TableName => "first_version_generation_plans"; }
public sealed class InjectionQuantityConfiguration : PeriodEntityConfiguration<InjectionQuantity> { protected override string TableName => "injection_quantities"; }
public sealed class PrimaryFrequencyCapacityPriceConfiguration : PeriodEntityConfiguration<PrimaryFrequencyCapacityPrice> { protected override string TableName => "primary_frequency_capacity_prices"; }
public sealed class SecondaryFrequencyCapacityPriceConfiguration : PeriodEntityConfiguration<SecondaryFrequencyCapacityPrice> { protected override string TableName => "secondary_frequency_capacity_prices"; }
public sealed class WindGenerationAndForecastConfiguration : PeriodEntityConfiguration<WindGenerationAndForecast> { protected override string TableName => "wind_generation_and_forecasts"; }

public sealed class SystemDirectionConfiguration : PeriodEntityConfiguration<SystemDirection>
{
    protected override string TableName => "system_directions";
    public override void Configure(EntityTypeBuilder<SystemDirection> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.Direction).HasMaxLength(100).IsRequired();
    }
}

public abstract class RawTransparencyConfiguration<TEntity> : PeriodEntityConfiguration<TEntity>
    where TEntity : BaseEntity, IRawTransparencyEntity
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.Payload).HasColumnType("jsonb").IsRequired();
    }
}

public sealed class FinalGenerationPlanConfiguration : PeriodEntityConfiguration<FinalGenerationPlan> { protected override string TableName => "final_generation_plans"; }
public sealed class NewInstalledCapacityConfiguration : KeyedPeriodEntityConfiguration<NewInstalledCapacity> { protected override string TableName => "new_installed_capacities"; }
public sealed class PlannedPowerOutageConfiguration : KeyedPeriodEntityConfiguration<PlannedPowerOutage> { protected override string TableName => "planned_power_outages"; }
public sealed class UnplannedPowerOutageConfiguration : KeyedPeriodEntityConfiguration<UnplannedPowerOutage> { protected override string TableName => "unplanned_power_outages"; }
public sealed class SgpPriceConfiguration : RawTransparencyConfiguration<SgpPrice> { protected override string TableName => "sgp_prices"; }
public sealed class AvailableInstalledCapacityConfiguration : PeriodEntityConfiguration<AvailableInstalledCapacity> { protected override string TableName => "available_installed_capacities"; }
public sealed class UnlicensedGenerationAmountConfiguration : PeriodEntityConfiguration<UnlicensedGenerationAmount> { protected override string TableName => "unlicensed_generation_amounts"; }
public sealed class RealTimeGenerationConfiguration : PeriodEntityConfiguration<RealTimeGeneration> { protected override string TableName => "real_time_generations"; }
public sealed class UpRegulationOrderSummaryConfiguration : PeriodEntityConfiguration<UpRegulationOrderSummary> { protected override string TableName => "up_regulation_order_summaries"; }
public sealed class DownRegulationOrderSummaryConfiguration : PeriodEntityConfiguration<DownRegulationOrderSummary> { protected override string TableName => "down_regulation_order_summaries"; }
public sealed class ClearingQuantityConfiguration : PeriodEntityConfiguration<ClearingQuantity> { protected override string TableName => "clearing_quantities"; }
public sealed class IdmWeightedAveragePriceConfiguration : PeriodEntityConfiguration<IdmWeightedAveragePrice> { protected override string TableName => "idm_weighted_average_prices"; }
public sealed class IdmMatchingQuantityConfiguration : KeyedPeriodEntityConfiguration<IdmMatchingQuantity> { protected override string TableName => "idm_matching_quantities"; }
public sealed class WithdrawalQuantityConfiguration : PeriodEntityConfiguration<WithdrawalQuantity> { protected override string TableName => "withdrawal_quantities"; }
public sealed class IdmContractSummaryConfiguration : RawTransparencyConfiguration<IdmContractSummary> { protected override string TableName => "idm_contract_summaries"; }
