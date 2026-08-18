using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608180003_RenameTransparencyJobCodes")]
public partial class RenameTransparencyJobCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE integration_jobs
            SET code = CASE id
                WHEN 1 THEN 'TRANSPARENCY_PTF'
                WHEN 2 THEN 'TRANSPARENCY_SMF'
            END
            WHERE id IN (1, 2);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE integration_jobs
            SET code = CASE id
                WHEN 1 THEN 'EPİAŞ_PTF'
                WHEN 2 THEN 'EPIAS_SMF'
            END
            WHERE id IN (1, 2);
            """);
    }
}
