using EnergyTrading.Domain;
using EnergyTrading.Domain.Transparency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnergyTrading.Infrastructure;

public sealed class TransparencyBackfillRunConfiguration
    : IEntityTypeConfiguration<TransparencyBackfillRun>
{
    public void Configure(EntityTypeBuilder<TransparencyBackfillRun> builder)
    {
        builder.ToTable("transparency_backfill_runs");
        builder.ConfigureBase();
        builder.Property(entity => entity.StartDate).HasColumnType("date");
        builder.Property(entity => entity.EndDate).HasColumnType("date");
        builder.HasIndex(entity => entity.StartedDate);
    }
}

public sealed class TransparencyBackfillItemConfiguration
    : IEntityTypeConfiguration<TransparencyBackfillItem>
{
    public void Configure(EntityTypeBuilder<TransparencyBackfillItem> builder)
    {
        builder.ToTable("transparency_backfill_items");
        builder.ConfigureBase();
        builder.Property(entity => entity.HangfireJobId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.JobCode).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.StartDate).HasColumnType("date");
        builder.Property(entity => entity.EndDate).HasColumnType("date");
        builder.HasOne(entity => entity.BackfillRun)
            .WithMany(entity => entity.Items)
            .HasForeignKey(entity => entity.BackfillRunId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(entity => entity.HangfireJobId).IsUnique();
        builder.HasIndex(entity => new { entity.BackfillRunId, entity.JobCode });
    }
}

public abstract class PeriodEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity, IPeriodEntity
{
    protected abstract string TableName { get; }

    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.ToTable(TableName, table =>
            table.HasCheckConstraint(
                $"ck_{TableName}_time_of_period_id",
                "time_of_period_id BETWEEN 1 AND 24"));
        builder.ConfigureBase();
        builder.Property(entity => entity.Date).HasColumnType("date");
        ConfigureIdentity(builder);
        foreach (var property in builder.Metadata.GetProperties().Where(property =>
                     property.ClrType == typeof(decimal) || Nullable.GetUnderlyingType(property.ClrType) == typeof(decimal)))
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
public sealed class WindGenerationAndForecastConfiguration : PeriodEntityConfiguration<WindGenerationAndForecast>
{
    protected override string TableName => "wind_generation_and_forecasts";

    protected override void ConfigureIdentity(EntityTypeBuilder<WindGenerationAndForecast> builder)
    {
        builder.Property(entity => entity.Hour).HasColumnType("time without time zone");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_wind_generation_and_forecasts_quarter", "quarter BETWEEN 1 AND 4");
        });
        builder.HasIndex(entity => new { entity.Date, entity.TimeOfPeriodId, entity.Quarter }).IsUnique();
    }
}

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
public sealed class IdmContractSummaryConfiguration : KeyedPeriodEntityConfiguration<IdmContractSummary> { protected override string TableName => "idm_contract_summaries"; }
