using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyTrading.Infrastructure.Migrations;

[DbContext(typeof(EnergyTradingDbContext))]
[Migration("202608190004_ConstrainTransparencyTimeOfPeriodId")]
public partial class ConstrainTransparencyTimeOfPeriodId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
                target record;
                constraint_name text;
                invalid_count bigint;
            BEGIN
                FOR target IN
                    SELECT table_schema, table_name
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND column_name = 'time_of_period_id'
                LOOP
                    constraint_name := left(
                        'ck_' || target.table_name || '_time_of_period_id',
                        63);

                    EXECUTE format(
                        'SELECT count(*) FROM %I.%I WHERE time_of_period_id NOT BETWEEN 1 AND 24',
                        target.table_schema,
                        target.table_name)
                    INTO invalid_count;

                    IF invalid_count > 0 THEN
                        RAISE EXCEPTION
                            'Table %.% contains % invalid time_of_period_id value(s).',
                            target.table_schema,
                            target.table_name,
                            invalid_count;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = constraint_name
                          AND conrelid = format('%I.%I', target.table_schema, target.table_name)::regclass
                    ) THEN
                        EXECUTE format(
                            'ALTER TABLE %I.%I ADD CONSTRAINT %I CHECK (time_of_period_id BETWEEN 1 AND 24)',
                            target.table_schema,
                            target.table_name,
                            constraint_name);
                    END IF;
                END LOOP;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
                target record;
                constraint_name text;
            BEGIN
                FOR target IN
                    SELECT table_schema, table_name
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND column_name = 'time_of_period_id'
                LOOP
                    constraint_name := left(
                        'ck_' || target.table_name || '_time_of_period_id',
                        63);
                    EXECUTE format(
                        'ALTER TABLE %I.%I DROP CONSTRAINT IF EXISTS %I',
                        target.table_schema,
                        target.table_name,
                        constraint_name);
                END LOOP;
            END $$;
            """);
    }
}
