using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608200004_SetContractPeriodsFromNames")]
public partial class SetContractPeriodsFromNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE idm_matching_quantities
            SET time_of_period_id = right(split_part(contract_name, '-', 1), 2)::integer + 1
            WHERE right(split_part(contract_name, '-', 1), 2) ~ '^[0-9]{2}$'
              AND right(split_part(contract_name, '-', 1), 2)::integer BETWEEN 0 AND 23;

            UPDATE idm_contract_summaries
            SET time_of_period_id = right(split_part(contract_name, '-', 1), 2)::integer + 1
            WHERE right(split_part(contract_name, '-', 1), 2) ~ '^[0-9]{2}$'
              AND right(split_part(contract_name, '-', 1), 2)::integer BETWEEN 0 AND 23;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE idm_matching_quantities SET time_of_period_id = 1;
            UPDATE idm_contract_summaries SET time_of_period_id = 1;
            """);
    }
}
