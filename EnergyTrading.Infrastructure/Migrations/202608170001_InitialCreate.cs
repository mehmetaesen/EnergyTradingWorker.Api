using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable
namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608170001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "integration_jobs", columns: table => new
        {
            id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false), code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true), cron_expression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false), queue_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            is_active = table.Column<bool>(type: "boolean", nullable: false), created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), updated_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => table.PrimaryKey("pk_integration_jobs", x => x.id));
        migrationBuilder.CreateTable(name: "market_clearing_prices", columns: table => new
        {
            id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), date = table.Column<DateOnly>(type: "date", nullable: false),
            time_of_period_id = table.Column<int>(type: "integer", nullable: false), price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false), price_usd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false), price_eur = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
            created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), updated_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => table.PrimaryKey("pk_market_clearing_prices", x => x.id));
        migrationBuilder.CreateTable(name: "system_marginal_prices", columns: table => new
        {
            id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            date = table.Column<DateOnly>(type: "date", nullable: false),
            time_of_period_id = table.Column<int>(type: "integer", nullable: false),
            price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
            created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            updated_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => table.PrimaryKey("pk_system_marginal_prices", x => x.id));
        migrationBuilder.CreateTable(name: "integration_job_logs", columns: table => new
        {
            id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), integration_job_id = table.Column<long>(type: "bigint", nullable: false), hangfire_job_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            correlation_id = table.Column<Guid>(type: "uuid", nullable: false), response_code = table.Column<int>(type: "integer", nullable: true), status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false), is_success = table.Column<bool>(type: "boolean", nullable: false), retry_count = table.Column<int>(type: "integer", nullable: false),
            error_description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true), started_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), completed_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), duration_milliseconds = table.Column<long>(type: "bigint", nullable: true),
            fetched_record_count = table.Column<int>(type: "integer", nullable: false), inserted_record_count = table.Column<int>(type: "integer", nullable: false), updated_record_count = table.Column<int>(type: "integer", nullable: false), created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), updated_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => { table.PrimaryKey("pk_integration_job_logs", x => x.id); table.ForeignKey("fk_integration_job_logs_integration_jobs_integration_job_id", x => x.integration_job_id, "integration_jobs", "id", onDelete: ReferentialAction.Restrict); });
        migrationBuilder.InsertData(
            table: "integration_jobs",
            columns: new[] { "id", "name", "code", "description", "cron_expression", "time_zone", "queue_name", "is_active", "created_date", "updated_date" },
            columnTypes: new[] { "bigint", "character varying(200)", "character varying(100)", "character varying(1000)", "character varying(100)", "character varying(100)", "character varying(50)", "boolean", "timestamp with time zone", "timestamp with time zone" },
            values: new object[] { 1L, "EPİAŞ Market Clearing Price", "TRANSPARENCY_PTF", "Fetches daily PTF values from the EPİAŞ Transparency Platform.", "0 15 * * *", "Europe/Istanbul", "transparency", true, new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null });
        migrationBuilder.InsertData(
            table: "integration_jobs",
            columns: new[] { "id", "name", "code", "description", "cron_expression", "time_zone", "queue_name", "is_active", "created_date", "updated_date" },
            columnTypes: new[] { "bigint", "character varying(200)", "character varying(100)", "character varying(1000)", "character varying(100)", "character varying(100)", "character varying(50)", "boolean", "timestamp with time zone", "timestamp with time zone" },
            values: new object[] { 2L, "EPİAŞ System Marginal Price", "TRANSPARENCY_SMF", "Fetches the current day's system marginal prices from the EPİAŞ Transparency Platform.", "0 * * * *", "Europe/Istanbul", "transparency", true, new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), null });
        migrationBuilder.CreateIndex("ix_integration_jobs_code", "integration_jobs", "code", unique: true);
        migrationBuilder.CreateIndex("ix_integration_job_logs_correlation_id", "integration_job_logs", "correlation_id"); migrationBuilder.CreateIndex("ix_integration_job_logs_integration_job_id_started_date", "integration_job_logs", new[] { "integration_job_id", "started_date" });
        migrationBuilder.CreateIndex("ix_market_clearing_prices_date_time_of_period_id", "market_clearing_prices", new[] { "date", "time_of_period_id" }, unique: true);
        migrationBuilder.CreateIndex("ix_system_marginal_prices_date_time_of_period_id", "system_marginal_prices", new[] { "date", "time_of_period_id" }, unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable("integration_job_logs"); migrationBuilder.DropTable("market_clearing_prices"); migrationBuilder.DropTable("system_marginal_prices"); migrationBuilder.DropTable("integration_jobs"); }
}
