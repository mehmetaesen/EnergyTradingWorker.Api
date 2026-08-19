using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608190001_AllowNullableTransparencyValues")]
public partial class AllowNullableTransparencyValues : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "total",
            table: "generation_plans",
            type: "numeric(18,6)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,6)");

        migrationBuilder.AlterColumn<decimal>(
            name: "total",
            table: "first_version_generation_plans",
            type: "numeric(18,6)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,6)");

        migrationBuilder.AlterColumn<decimal>(
            name: "total",
            table: "final_generation_plans",
            type: "numeric(18,6)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,6)");

        migrationBuilder.AlterColumn<decimal>(
            name: "generation",
            table: "wind_generation_and_forecasts",
            type: "numeric(18,6)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,6)");

        migrationBuilder.AlterColumn<decimal>(
            name: "total",
            table: "available_installed_capacities",
            type: "numeric(18,6)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,6)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE generation_plans SET total = 0 WHERE total IS NULL;");
        migrationBuilder.Sql("UPDATE first_version_generation_plans SET total = 0 WHERE total IS NULL;");
        migrationBuilder.Sql("UPDATE final_generation_plans SET total = 0 WHERE total IS NULL;");
        migrationBuilder.Sql("UPDATE wind_generation_and_forecasts SET generation = 0 WHERE generation IS NULL;");
        migrationBuilder.Sql("UPDATE available_installed_capacities SET total = 0 WHERE total IS NULL;");

        migrationBuilder.AlterColumn<decimal>(name: "total", table: "generation_plans", type: "numeric(18,6)", nullable: false, oldClrType: typeof(decimal), oldType: "numeric(18,6)", oldNullable: true);
        migrationBuilder.AlterColumn<decimal>(name: "total", table: "first_version_generation_plans", type: "numeric(18,6)", nullable: false, oldClrType: typeof(decimal), oldType: "numeric(18,6)", oldNullable: true);
        migrationBuilder.AlterColumn<decimal>(name: "total", table: "final_generation_plans", type: "numeric(18,6)", nullable: false, oldClrType: typeof(decimal), oldType: "numeric(18,6)", oldNullable: true);
        migrationBuilder.AlterColumn<decimal>(name: "generation", table: "wind_generation_and_forecasts", type: "numeric(18,6)", nullable: false, oldClrType: typeof(decimal), oldType: "numeric(18,6)", oldNullable: true);
        migrationBuilder.AlterColumn<decimal>(name: "total", table: "available_installed_capacities", type: "numeric(18,6)", nullable: false, oldClrType: typeof(decimal), oldType: "numeric(18,6)", oldNullable: true);
    }
}
