using EnergyTrading.Application;
using EnergyTrading.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.RegularExpressions;

namespace EnergyTrading.Infrastructure;

internal static class ConfigurationExtensions
{
    public static void ConfigureBase<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : BaseEntity
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedDate).IsRequired();
    }

    public static void UsePostgreSqlSnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
            foreach (var property in entity.GetProperties()) property.SetColumnName(ToSnakeCase(property.Name));
            foreach (var key in entity.GetKeys()) key.SetName(ToSnakeCase(key.GetName()!));
            foreach (var foreignKey in entity.GetForeignKeys()) foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
            foreach (var index in entity.GetIndexes()) index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
        }
    }

    private static string ToSnakeCase(string value) => Regex.Replace(value, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
}

public sealed class IntegrationJobConfiguration : IEntityTypeConfiguration<IntegrationJob>
{
    public void Configure(EntityTypeBuilder<IntegrationJob> b)
    {
        b.ToTable("integration_jobs"); b.ConfigureBase();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired(); b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000); b.Property(x => x.CronExpression).HasMaxLength(100).IsRequired();
        b.Property(x => x.TimeZone).HasMaxLength(100).IsRequired(); b.Property(x => x.QueueName).HasMaxLength(50).IsRequired();
        b.Property(x => x.TableName).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.HasData(new IntegrationJob { Id = 1, Name = "Piyasa Takas Fiyatı (PTF)", Code = MarketClearingPriceJob.Code,
            Description = "Fetches daily PTF values from the EPİAŞ Transparency Platform.", CronExpression = "0 15 * * *",
            TimeZone = "Europe/Istanbul", QueueName = "transparency", TableName = "market_clearing_prices", IsActive = true, CreatedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) });
        b.HasData(new IntegrationJob { Id = 2, Name = "Sistem Marjinal Fiyatı (SMF)", Code = SystemMarginalPriceJob.Code,
            Description = "Fetches the current day's system marginal prices from the EPİAŞ Transparency Platform.", CronExpression = "5 * * * *",
            TimeZone = "Europe/Istanbul", QueueName = "transparency", TableName = "system_marginal_prices", IsActive = true, CreatedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) });
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        b.HasData(
            Job(3, "Yük Tahmin Planı", LoadEstimationPlanJob.Code, "Bir sonraki günün saatlik yük tahmin planını getirir.", "5 14 * * *", "load_estimation_plans", created),
            Job(4, "RES Üretim ve Tahmin", WindGenerationForecastJob.Code, "RES üretim ve tahmin verilerini getirir.", "*/10 * * * *", "wind_generation_and_forecasts", created),
            Job(5, "SFK Kapasite Fiyatı", SecondaryFrequencyCapacityPriceJob.Code, "Saatlik SFK kapasite fiyatlarını getirir.", "20 * * * *", "secondary_frequency_capacity_prices", created),
            Job(6, "PFK Kapasite Fiyatı", PrimaryFrequencyCapacityPriceJob.Code, "Saatlik PFK kapasite fiyatlarını getirir.", "15 * * * *", "primary_frequency_capacity_prices", created),
            Job(7, "Uzlaştırma Esas Veriş Miktarı", InjectionQuantityJob.Code, "Saatlik UEVM verilerini getirir.", "30 2 * * *", "injection_quantities", created),
            Job(8, "Sistem Yönü", SystemDirectionJob.Code, "Saatlik sistem yönü verilerini getirir.", "10 * * * *", "system_directions", created),
            Job(9, "KGÜP İlk Versiyon", FirstVersionGenerationPlanJob.Code, "KGÜP ilk versiyon verilerini getirir.", "20 16 * * *", "first_version_generation_plans", created),
            Job(10, "KGÜP", GenerationPlanJob.Code, "Kesinleşmiş günlük üretim planını getirir.", "15 16 * * *", "generation_plans", created),
            Job(11, "Gerçek Zamanlı Tüketim", RealTimeConsumptionJob.Code, "Saatlik gerçek zamanlı tüketim verilerini getirir.", "15 * * * *", "real_time_consumptions", created));
    }

    private static IntegrationJob Job(long id, string name, string code, string description, string cron, string tableName, DateTimeOffset created) =>
        new() { Id = id, Name = name, Code = code, Description = description, CronExpression = cron, TimeZone = "Europe/Istanbul", QueueName = "transparency", TableName = tableName, IsActive = true, CreatedDate = created };
}

public sealed class IntegrationJobLogConfiguration : IEntityTypeConfiguration<IntegrationJobLog>
{
    public void Configure(EntityTypeBuilder<IntegrationJobLog> b)
    {
        b.ToTable("integration_job_logs"); b.ConfigureBase();
        b.Property(x => x.HangfireJobId).HasMaxLength(100); b.Property(x => x.ErrorDescription).HasMaxLength(4000);
        b.Property(x => x.ResponseBody).HasColumnType("text");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.HasOne(x => x.IntegrationJob).WithMany(x => x.Logs).HasForeignKey(x => x.IntegrationJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.IntegrationJobId, x.StartedDate }); b.HasIndex(x => x.CorrelationId);
    }
}

public sealed class MarketClearingPriceConfiguration : IEntityTypeConfiguration<MarketClearingPrice>
{
    public void Configure(EntityTypeBuilder<MarketClearingPrice> b)
    {
        b.ToTable("market_clearing_prices"); b.ConfigureBase();
        b.Property(x => x.Date).HasColumnType("date");
        b.Property(x => x.Price).HasPrecision(18, 6); b.Property(x => x.PriceUsd).HasPrecision(18, 6); b.Property(x => x.PriceEur).HasPrecision(18, 6);
        b.HasIndex(x => new { x.Date, x.TimeOfPeriodId }).IsUnique();
    }
}

public sealed class SystemMarginalPriceConfiguration : IEntityTypeConfiguration<SystemMarginalPrice>
{
    public void Configure(EntityTypeBuilder<SystemMarginalPrice> b)
    {
        b.ToTable("system_marginal_prices");
        b.ConfigureBase();
        b.Property(x => x.Date).HasColumnType("date");
        b.Property(x => x.Price).HasPrecision(18, 6);
        b.HasIndex(x => new { x.Date, x.TimeOfPeriodId }).IsUnique();
    }
}
