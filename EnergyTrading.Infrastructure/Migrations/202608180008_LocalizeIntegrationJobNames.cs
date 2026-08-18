using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608180008_LocalizeIntegrationJobNames")]
public partial class LocalizeIntegrationJobNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE integration_jobs
            SET name = 'Piyasa Takas Fiyatı (PTF)'
            WHERE code = 'TRANSPARENCY_PTF';

            UPDATE integration_jobs
            SET name = 'Sistem Marjinal Fiyatı (SMF)'
            WHERE code = 'TRANSPARENCY_SMF';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE integration_jobs
            SET name = 'EPİAŞ Market Clearing Price'
            WHERE code = 'TRANSPARENCY_PTF';

            UPDATE integration_jobs
            SET name = 'EPİAŞ System Marginal Price'
            WHERE code = 'TRANSPARENCY_SMF';
            """);
    }
}
