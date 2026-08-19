using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608200001_AllowNullableWindForecastQuantiles")]
public partial class AllowNullableWindForecastQuantiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE wind_generation_and_forecasts
                ALTER COLUMN quantile5 DROP NOT NULL,
                ALTER COLUMN quantile25 DROP NOT NULL,
                ALTER COLUMN quantile75 DROP NOT NULL,
                ALTER COLUMN quantile95 DROP NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM wind_generation_and_forecasts
            WHERE quantile5 IS NULL OR quantile25 IS NULL OR quantile75 IS NULL OR quantile95 IS NULL;

            ALTER TABLE wind_generation_and_forecasts
                ALTER COLUMN quantile5 SET NOT NULL,
                ALTER COLUMN quantile25 SET NOT NULL,
                ALTER COLUMN quantile75 SET NOT NULL,
                ALTER COLUMN quantile95 SET NOT NULL;
            """);
    }
}
