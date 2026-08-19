using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608200002_AllowNullableWindForecast")]
public partial class AllowNullableWindForecast : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE wind_generation_and_forecasts
                ALTER COLUMN forecast DROP NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM wind_generation_and_forecasts WHERE forecast IS NULL;
            ALTER TABLE wind_generation_and_forecasts
                ALTER COLUMN forecast SET NOT NULL;
            """);
    }
}
