using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608190005_AddTransparencyBackfillTracking")]
public partial class AddTransparencyBackfillTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "transparency_backfill_runs",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation(
                        "Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                start_date = table.Column<DateOnly>(type: "date", nullable: false),
                end_date = table.Column<DateOnly>(type: "date", nullable: false),
                total_job_count = table.Column<int>(type: "integer", nullable: false),
                started_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("pk_transparency_backfill_runs", x => x.id));

        migrationBuilder.CreateTable(
            name: "transparency_backfill_items",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation(
                        "Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                backfill_run_id = table.Column<long>(type: "bigint", nullable: false),
                hangfire_job_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                job_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                start_date = table.Column<DateOnly>(type: "date", nullable: false),
                end_date = table.Column<DateOnly>(type: "date", nullable: false),
                created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_transparency_backfill_items", x => x.id);
                table.ForeignKey(
                    name: "fk_transparency_backfill_items_transparency_backfill_runs_backfill_run_id",
                    column: x => x.backfill_run_id,
                    principalTable: "transparency_backfill_runs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_transparency_backfill_runs_started_date",
            table: "transparency_backfill_runs",
            column: "started_date");
        migrationBuilder.CreateIndex(
            name: "ix_transparency_backfill_items_hangfire_job_id",
            table: "transparency_backfill_items",
            column: "hangfire_job_id",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "ix_transparency_backfill_items_backfill_run_id_job_code",
            table: "transparency_backfill_items",
            columns: ["backfill_run_id", "job_code"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "transparency_backfill_items");
        migrationBuilder.DropTable(name: "transparency_backfill_runs");
    }
}
