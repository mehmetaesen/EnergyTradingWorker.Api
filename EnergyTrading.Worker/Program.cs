using EnergyTrading.Application;
using EnergyTrading.Infrastructure;
using EnergyTrading.Worker;
using Scheduled = EnergyTrading.Worker.Transparency;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSerilog((services, logger) => logger.ReadFrom.Configuration(builder.Configuration).WriteTo.Console());
builder.Services.AddEnergyTradingInfrastructure(builder.Configuration);

var connection = builder.Configuration.GetConnectionString("EnergyTrading")
    ?? throw new InvalidOperationException("ConnectionStrings:EnergyTrading is required.");
builder.Services.AddHangfire(configuration => configuration
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connection)));
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = builder.Configuration.GetValue("Hangfire:WorkerCount", 5);
    options.Queues = ["transparency", "default"];
});
foreach (var jobType in typeof(Scheduled.MarketClearingPriceJob).Assembly.GetTypes()
             .Where(type => type is { IsClass: true, IsAbstract: false } && type.Namespace == "EnergyTrading.Worker.Transparency" && type.Name.EndsWith("Job", StringComparison.Ordinal)))
    builder.Services.AddScoped(jobType);

var app = builder.Build();
app.UseMiddleware<OperationsAuthenticationMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHangfireDashboard("/hangfire");

app.MapGet("/api/jobs", async (EnergyTradingDbContext db, CancellationToken cancellationToken) =>
    await db.IntegrationJobs
        .AsNoTracking()
        .Where(job => job.IsActive)
        .OrderBy(job => job.Name)
        .Select(job => new { job.Code, job.Name, job.Description, job.QueueName, job.TableName })
        .ToListAsync(cancellationToken));

app.MapPost("/api/jobs/enqueue", (
    ManualJobRequest request,
    HttpRequest httpRequest,
    IBackgroundJobClient backgroundJobs,
    ITurkeyClock clock) =>
{
    if (!string.Equals(httpRequest.Headers["X-Requested-With"], "EnergyTradingOperations", StringComparison.Ordinal))
        return Results.BadRequest(new { Message = "Invalid operations request." });
    if (request.EndDate < request.StartDate)
        return Results.BadRequest(new { Message = "Bitiş tarihi başlangıç tarihinden önce olamaz." });
    if (request.EndDate > request.StartDate.AddMonths(1))
        return Results.BadRequest(new { Message = "Tarih aralığı en fazla bir takvim ayı olabilir." });
    if (request.JobCode == SystemMarginalPriceJob.Code
        && (request.StartDate > clock.Today || request.EndDate > clock.Today))
        return Results.BadRequest(new { Message = "Sistem Marjinal Fiyatı gelecek tarih için çalıştırılamaz." });

    var backgroundJobId = request.JobCode switch
    {
        MarketClearingPriceJob.Code => backgroundJobs.Enqueue<Scheduled.MarketClearingPriceJob>(job =>
            job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        SystemMarginalPriceJob.Code => backgroundJobs.Enqueue<Scheduled.SystemMarginalPriceJob>(job =>
            job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        LoadEstimationPlanJob.Code => backgroundJobs.Enqueue<Scheduled.LoadEstimationPlanJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        RealTimeConsumptionJob.Code => backgroundJobs.Enqueue<Scheduled.RealTimeConsumptionJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        GenerationPlanJob.Code => backgroundJobs.Enqueue<Scheduled.GenerationPlanJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        FirstVersionGenerationPlanJob.Code => backgroundJobs.Enqueue<Scheduled.FirstVersionGenerationPlanJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        InjectionQuantityJob.Code => backgroundJobs.Enqueue<Scheduled.InjectionQuantityJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        PrimaryFrequencyCapacityPriceJob.Code => backgroundJobs.Enqueue<Scheduled.PrimaryFrequencyCapacityPriceJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        SecondaryFrequencyCapacityPriceJob.Code => backgroundJobs.Enqueue<Scheduled.SecondaryFrequencyCapacityPriceJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        SystemDirectionJob.Code => backgroundJobs.Enqueue<Scheduled.SystemDirectionJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        WindGenerationForecastJob.Code => backgroundJobs.Enqueue<Scheduled.WindGenerationForecastJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        FinalGenerationPlanJob.Code => backgroundJobs.Enqueue<Scheduled.FinalGenerationPlanJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        NewInstalledCapacityJob.Code => backgroundJobs.Enqueue<Scheduled.NewInstalledCapacityJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        PlannedPowerOutageJob.Code => backgroundJobs.Enqueue<Scheduled.PlannedPowerOutageJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        UnplannedPowerOutageJob.Code => backgroundJobs.Enqueue<Scheduled.UnplannedPowerOutageJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        SgpPriceJob.Code => backgroundJobs.Enqueue<Scheduled.SgpPriceJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        AvailableInstalledCapacityJob.Code => backgroundJobs.Enqueue<Scheduled.AvailableInstalledCapacityJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        UnlicensedGenerationJob.Code => backgroundJobs.Enqueue<Scheduled.UnlicensedGenerationJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        RealTimeGenerationJob.Code => backgroundJobs.Enqueue<Scheduled.RealTimeGenerationJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        OrderSummaryUpJob.Code => backgroundJobs.Enqueue<Scheduled.OrderSummaryUpJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        OrderSummaryDownJob.Code => backgroundJobs.Enqueue<Scheduled.OrderSummaryDownJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        ClearingQuantityJob.Code => backgroundJobs.Enqueue<Scheduled.ClearingQuantityJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        IdmWeightedAveragePriceJob.Code => backgroundJobs.Enqueue<Scheduled.IdmWeightedAveragePriceJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        IdmMatchingQuantityJob.Code => backgroundJobs.Enqueue<Scheduled.IdmMatchingQuantityJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        WithdrawalQuantityJob.Code => backgroundJobs.Enqueue<Scheduled.WithdrawalQuantityJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        IdmContractSummaryJob.Code => backgroundJobs.Enqueue<Scheduled.IdmContractSummaryJob>(job => job.ExecuteAsync(request.StartDate, request.EndDate, null, CancellationToken.None)),
        _ => null
    };

    return backgroundJobId is null
        ? Results.BadRequest(new { Message = "Seçilen job manuel çalıştırmayı desteklemiyor." })
        : Results.Ok(new { JobId = backgroundJobId, Message = "Job queue'ya eklendi." });
});

await using (var scope = app.Services.CreateAsyncScope())
{
    if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
    {
        var db = scope.ServiceProvider.GetRequiredService<EnergyTradingDbContext>();
        await db.Database.MigrateAsync();
    }

    var manager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(builder.Configuration["Hangfire:TimeZone"] ?? "Europe/Istanbul");
    manager.RemoveIfExists("EPİAŞ_PTF");
    manager.RemoveIfExists("EPIAS_SMF");
    manager.RemoveIfExists("SEFFAFLIK_PTF");
    manager.RemoveIfExists("SEFFAFLIK_SMF");

    var recurringJobCodes = new[]
    {
        MarketClearingPriceJob.Code,
        SystemMarginalPriceJob.Code,
        LoadEstimationPlanJob.Code,
        RealTimeConsumptionJob.Code,
        GenerationPlanJob.Code,
        FirstVersionGenerationPlanJob.Code,
        InjectionQuantityJob.Code,
        PrimaryFrequencyCapacityPriceJob.Code,
        SecondaryFrequencyCapacityPriceJob.Code,
        SystemDirectionJob.Code,
        WindGenerationForecastJob.Code,
        FinalGenerationPlanJob.Code,
        NewInstalledCapacityJob.Code,
        PlannedPowerOutageJob.Code,
        UnplannedPowerOutageJob.Code,
        AvailableInstalledCapacityJob.Code,
        UnlicensedGenerationJob.Code,
        RealTimeGenerationJob.Code,
        OrderSummaryUpJob.Code,
        OrderSummaryDownJob.Code,
        ClearingQuantityJob.Code,
        IdmWeightedAveragePriceJob.Code,
        IdmMatchingQuantityJob.Code,
        WithdrawalQuantityJob.Code
    };

    if (!builder.Configuration.GetValue("Hangfire:EnableRecurringJobs", false))
    {
        foreach (var jobCode in recurringJobCodes)
            manager.RemoveIfExists(jobCode);
    }
    else
    {
        manager.AddOrUpdate<Scheduled.MarketClearingPriceJob>(
            MarketClearingPriceJob.Code,
            job => job.ExecuteAsync(null, null, null, CancellationToken.None),
            builder.Configuration["Hangfire:MarketClearingPriceCron"] ?? "0 15 * * *",
            new RecurringJobOptions { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.SystemMarginalPriceJob>(
            SystemMarginalPriceJob.Code,
            job => job.ExecuteAsync(null, null, null, CancellationToken.None),
            builder.Configuration["Hangfire:SystemMarginalPriceCron"] ?? "5 * * * *",
            new RecurringJobOptions { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.LoadEstimationPlanJob>(LoadEstimationPlanJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), builder.Configuration["Hangfire:LoadEstimationPlanCron"] ?? "5 14 * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.RealTimeConsumptionJob>(RealTimeConsumptionJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), builder.Configuration["Hangfire:RealTimeConsumptionCron"] ?? "15 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.GenerationPlanJob>(GenerationPlanJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), builder.Configuration["Hangfire:GenerationPlanCron"] ?? "15 16 * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.FirstVersionGenerationPlanJob>(FirstVersionGenerationPlanJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), builder.Configuration["Hangfire:FirstVersionGenerationPlanCron"] ?? "20 16 * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.InjectionQuantityJob>(InjectionQuantityJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), builder.Configuration["Hangfire:InjectionQuantityCron"] ?? "30 2 * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.PrimaryFrequencyCapacityPriceJob>(PrimaryFrequencyCapacityPriceJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), builder.Configuration["Hangfire:PrimaryFrequencyPriceCron"] ?? "15 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.SecondaryFrequencyCapacityPriceJob>(SecondaryFrequencyCapacityPriceJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), builder.Configuration["Hangfire:SecondaryFrequencyPriceCron"] ?? "20 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.SystemDirectionJob>(SystemDirectionJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), builder.Configuration["Hangfire:SystemDirectionCron"] ?? "10 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.WindGenerationForecastJob>(WindGenerationForecastJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), builder.Configuration["Hangfire:WindGenerationForecastCron"] ?? "*/10 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.FinalGenerationPlanJob>(FinalGenerationPlanJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "30 16 * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.NewInstalledCapacityJob>(NewInstalledCapacityJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "0 6 * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.PlannedPowerOutageJob>(PlannedPowerOutageJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "0 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.UnplannedPowerOutageJob>(UnplannedPowerOutageJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "30 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.AvailableInstalledCapacityJob>(AvailableInstalledCapacityJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "25 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.UnlicensedGenerationJob>(UnlicensedGenerationJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "0 3 * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.RealTimeGenerationJob>(RealTimeGenerationJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "15 3 * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.OrderSummaryUpJob>(OrderSummaryUpJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "35 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.OrderSummaryDownJob>(OrderSummaryDownJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "40 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.ClearingQuantityJob>(ClearingQuantityJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "10 15 * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.IdmWeightedAveragePriceJob>(IdmWeightedAveragePriceJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "45 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.IdmMatchingQuantityJob>(IdmMatchingQuantityJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "50 * * * *", new() { TimeZone = timeZone });
        manager.AddOrUpdate<Scheduled.WithdrawalQuantityJob>(WithdrawalQuantityJob.Code, job => job.ExecuteAsync(null, null, null, CancellationToken.None), "30 3 * * *", new() { TimeZone = timeZone });
    }
}

await app.RunAsync();

public sealed record ManualJobRequest(string JobCode, DateOnly StartDate, DateOnly EndDate);
