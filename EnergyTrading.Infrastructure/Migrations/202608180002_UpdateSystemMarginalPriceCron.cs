using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608180002_UpdateSystemMarginalPriceCron")]
public partial class UpdateSystemMarginalPriceCron : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "UPDATE integration_jobs SET cron_expression = '5 * * * *' WHERE id = 2;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "UPDATE integration_jobs SET cron_expression = '0 * * * *' WHERE id = 2;");
    }
}
