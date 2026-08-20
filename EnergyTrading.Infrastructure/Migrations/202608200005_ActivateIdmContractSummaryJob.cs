using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608200005_ActivateIdmContractSummaryJob")]
public partial class ActivateIdmContractSummaryJob : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE integration_jobs
            SET is_active = true,
                description = 'GİP kontrat özetlerini typed alanlarla getirir.'
            WHERE code = 'TRANSPARENCY_IDM_CONTRACT_SUMMARY';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE integration_jobs
            SET is_active = false,
                description = 'GİP kontrat özetlerini getirir; kontrat doğrulaması bekleniyor.'
            WHERE code = 'TRANSPARENCY_IDM_CONTRACT_SUMMARY';
            """);
    }
}
