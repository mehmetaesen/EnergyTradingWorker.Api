using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608180006_RenameTransparencyQueue")]
public partial class RenameTransparencyQueue : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("UPDATE integration_jobs SET queue_name = 'transparency' WHERE queue_name = 'epias';");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("UPDATE integration_jobs SET queue_name = 'epias' WHERE queue_name = 'transparency';");
}
