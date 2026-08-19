using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608190002_AllowNullableGenerationPlanValues")]
public partial class AllowNullableGenerationPlanValues : Migration
{
    private const string Tables =
        "ARRAY['generation_plans','first_version_generation_plans','final_generation_plans','available_installed_capacities']";

    private const string Columns =
        "ARRAY['river','dam','biomass','other','natural_gas','fuel_oil','solar','imported_coal','geothermal','lignite','naphtha','wind','hard_coal']";

    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(
            $$"""
            DO $$
            DECLARE table_name text; column_name text;
            BEGIN
              FOREACH table_name IN ARRAY {{Tables}} LOOP
                FOREACH column_name IN ARRAY {{Columns}} LOOP
                  EXECUTE format('ALTER TABLE %I ALTER COLUMN %I DROP NOT NULL', table_name, column_name);
                END LOOP;
              END LOOP;
            END $$;
            """);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(
            $$"""
            DO $$
            DECLARE table_name text; column_name text;
            BEGIN
              FOREACH table_name IN ARRAY {{Tables}} LOOP
                FOREACH column_name IN ARRAY {{Columns}} LOOP
                  EXECUTE format('UPDATE %I SET %I = 0 WHERE %I IS NULL', table_name, column_name, column_name);
                  EXECUTE format('ALTER TABLE %I ALTER COLUMN %I SET NOT NULL', table_name, column_name);
                END LOOP;
              END LOOP;
            END $$;
            """);
}
