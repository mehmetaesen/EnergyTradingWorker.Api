using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608180009_AddJobTableNameAndRenameDataTables")]
public partial class AddJobTableNameAndRenameDataTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS final_generation_plan_snapshots RENAME TO final_generation_plans;
            ALTER TABLE IF EXISTS new_installed_capacity_snapshots RENAME TO new_installed_capacities;
            ALTER TABLE IF EXISTS planned_power_outage_snapshots RENAME TO planned_power_outages;
            ALTER TABLE IF EXISTS unplanned_power_outage_snapshots RENAME TO unplanned_power_outages;
            ALTER TABLE IF EXISTS sgp_price_snapshots RENAME TO sgp_prices;
            ALTER TABLE IF EXISTS available_installed_capacity_snapshots RENAME TO available_installed_capacities;
            ALTER TABLE IF EXISTS unlicensed_generation_snapshots RENAME TO unlicensed_generation_amounts;
            ALTER TABLE IF EXISTS real_time_generation_snapshots RENAME TO real_time_generations;
            ALTER TABLE IF EXISTS order_summary_up_snapshots RENAME TO up_regulation_order_summaries;
            ALTER TABLE IF EXISTS order_summary_down_snapshots RENAME TO down_regulation_order_summaries;
            ALTER TABLE IF EXISTS clearing_quantity_snapshots RENAME TO clearing_quantities;
            ALTER TABLE IF EXISTS idm_weighted_average_price_snapshots RENAME TO idm_weighted_average_prices;
            ALTER TABLE IF EXISTS idm_matching_quantity_snapshots RENAME TO idm_matching_quantities;
            ALTER TABLE IF EXISTS withdrawal_quantity_snapshots RENAME TO withdrawal_quantities;
            ALTER TABLE IF EXISTS idm_contract_summary_snapshots RENAME TO idm_contract_summaries;

            ALTER TABLE integration_jobs ADD COLUMN IF NOT EXISTS table_name varchar(200) NOT NULL DEFAULT '';

            UPDATE integration_jobs SET table_name = CASE code
              WHEN 'TRANSPARENCY_PTF' THEN 'market_clearing_prices'
              WHEN 'TRANSPARENCY_SMF' THEN 'system_marginal_prices'
              WHEN 'TRANSPARENCY_LOAD_ESTIMATION_PLAN' THEN 'load_estimation_plans'
              WHEN 'TRANSPARENCY_RES_GENERATION_FORECAST' THEN 'wind_generation_and_forecasts'
              WHEN 'TRANSPARENCY_SFK_PRICE' THEN 'secondary_frequency_capacity_prices'
              WHEN 'TRANSPARENCY_PFK_PRICE' THEN 'primary_frequency_capacity_prices'
              WHEN 'TRANSPARENCY_UEVM' THEN 'injection_quantities'
              WHEN 'TRANSPARENCY_SYSTEM_DIRECTION' THEN 'system_directions'
              WHEN 'TRANSPARENCY_KGUP_FIRST_VERSION' THEN 'first_version_generation_plans'
              WHEN 'TRANSPARENCY_KGUP' THEN 'generation_plans'
              WHEN 'TRANSPARENCY_REALTIME_CONSUMPTION' THEN 'real_time_consumptions'
              WHEN 'TRANSPARENCY_FINAL_GENERATION_PLAN' THEN 'final_generation_plans'
              WHEN 'TRANSPARENCY_NEW_INSTALLED_CAPACITY' THEN 'new_installed_capacities'
              WHEN 'TRANSPARENCY_PLANNED_POWER_OUTAGE' THEN 'planned_power_outages'
              WHEN 'TRANSPARENCY_UNPLANNED_POWER_OUTAGE' THEN 'unplanned_power_outages'
              WHEN 'TRANSPARENCY_SGP_PRICE' THEN 'sgp_prices'
              WHEN 'TRANSPARENCY_AVAILABLE_INSTALLED_CAPACITY' THEN 'available_installed_capacities'
              WHEN 'TRANSPARENCY_UNLICENSED_GENERATION' THEN 'unlicensed_generation_amounts'
              WHEN 'TRANSPARENCY_REALTIME_GENERATION' THEN 'real_time_generations'
              WHEN 'TRANSPARENCY_ORDER_SUMMARY_UP' THEN 'up_regulation_order_summaries'
              WHEN 'TRANSPARENCY_ORDER_SUMMARY_DOWN' THEN 'down_regulation_order_summaries'
              WHEN 'TRANSPARENCY_CLEARING_QUANTITY' THEN 'clearing_quantities'
              WHEN 'TRANSPARENCY_IDM_WEIGHTED_AVERAGE_PRICE' THEN 'idm_weighted_average_prices'
              WHEN 'TRANSPARENCY_IDM_MATCHING_QUANTITY' THEN 'idm_matching_quantities'
              WHEN 'TRANSPARENCY_UECM' THEN 'withdrawal_quantities'
              WHEN 'TRANSPARENCY_IDM_CONTRACT_SUMMARY' THEN 'idm_contract_summaries'
              ELSE table_name
            END;

            ALTER TABLE integration_jobs ALTER COLUMN table_name DROP DEFAULT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE final_generation_plans RENAME TO final_generation_plan_snapshots;
            ALTER TABLE new_installed_capacities RENAME TO new_installed_capacity_snapshots;
            ALTER TABLE planned_power_outages RENAME TO planned_power_outage_snapshots;
            ALTER TABLE unplanned_power_outages RENAME TO unplanned_power_outage_snapshots;
            ALTER TABLE sgp_prices RENAME TO sgp_price_snapshots;
            ALTER TABLE available_installed_capacities RENAME TO available_installed_capacity_snapshots;
            ALTER TABLE unlicensed_generation_amounts RENAME TO unlicensed_generation_snapshots;
            ALTER TABLE real_time_generations RENAME TO real_time_generation_snapshots;
            ALTER TABLE up_regulation_order_summaries RENAME TO order_summary_up_snapshots;
            ALTER TABLE down_regulation_order_summaries RENAME TO order_summary_down_snapshots;
            ALTER TABLE clearing_quantities RENAME TO clearing_quantity_snapshots;
            ALTER TABLE idm_weighted_average_prices RENAME TO idm_weighted_average_price_snapshots;
            ALTER TABLE idm_matching_quantities RENAME TO idm_matching_quantity_snapshots;
            ALTER TABLE withdrawal_quantities RENAME TO withdrawal_quantity_snapshots;
            ALTER TABLE idm_contract_summaries RENAME TO idm_contract_summary_snapshots;
            ALTER TABLE integration_jobs DROP COLUMN table_name;
            """);
    }
}
