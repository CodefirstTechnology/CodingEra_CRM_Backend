using System;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <summary>
    /// Migration: AddGroup1CoreInspectionPipeline
    /// Adds split-lot quantity fields, replicate sample measurements table, 
    /// precision tolerances, and UoM attributes across Group 1 Quality Control tables.
    /// </summary>
    [DbContext(typeof(ERPDbContext))]
    [Migration("20261001140000_AddGroup1CoreInspectionPipeline")]
    public partial class AddGroup1CoreInspectionPipeline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add split-lot quantity columns to incoming_inspections
            migrationBuilder.Sql(@"
                ALTER TABLE incoming_inspections
                ADD COLUMN IF NOT EXISTS ""ReworkQuantity"" NUMERIC(14, 4) NOT NULL DEFAULT 0.0000,
                ADD COLUMN IF NOT EXISTS ""ScrapQuantity"" NUMERIC(14, 4) NOT NULL DEFAULT 0.0000,
                ADD COLUMN IF NOT EXISTS ""RtvQuantity"" NUMERIC(14, 4) NOT NULL DEFAULT 0.0000;
            ");

            // 2. Add UoM, tolerances, and statistical metrics to incoming_checklist_items
            migrationBuilder.Sql(@"
                ALTER TABLE incoming_checklist_items
                ADD COLUMN IF NOT EXISTS ""UoM"" VARCHAR(32) NOT NULL DEFAULT 'mm',
                ADD COLUMN IF NOT EXISTS ""TargetValue"" NUMERIC(14, 4) NULL,
                ADD COLUMN IF NOT EXISTS ""MinTolerance"" NUMERIC(14, 4) NULL,
                ADD COLUMN IF NOT EXISTS ""MaxTolerance"" NUMERIC(14, 4) NULL,
                ADD COLUMN IF NOT EXISTS ""MeanValue"" NUMERIC(14, 4) NULL,
                ADD COLUMN IF NOT EXISTS ""StdDeviation"" NUMERIC(14, 4) NULL;
            ");

            // 3. Create dedicated replicate measurements table incoming_inspection_samples
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS incoming_inspection_samples (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""IncomingChecklistItemId"" INT NOT NULL REFERENCES incoming_checklist_items(""Id"") ON DELETE CASCADE,
                    ""SampleIndex"" INT NOT NULL,
                    ""ObservedNumericValue"" NUMERIC(14, 4) NOT NULL,
                    ""IsWithinLimits"" BOOLEAN NOT NULL DEFAULT TRUE,
                    ""MeasurementToolId"" VARCHAR(64) NULL,
                    ""CapturedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ");

            // 4. Add UoM and tolerance bounds to in_process_checks
            migrationBuilder.Sql(@"
                ALTER TABLE in_process_checks
                ADD COLUMN IF NOT EXISTS ""UoM"" VARCHAR(32) NOT NULL DEFAULT 'mm',
                ADD COLUMN IF NOT EXISTS ""TargetValue"" NUMERIC(14, 4) NULL,
                ADD COLUMN IF NOT EXISTS ""MinTolerance"" NUMERIC(14, 4) NULL,
                ADD COLUMN IF NOT EXISTS ""MaxTolerance"" NUMERIC(14, 4) NULL,
                ADD COLUMN IF NOT EXISTS ""ActualNumericValue"" NUMERIC(14, 4) NULL;
            ");

            // 5. Add UoM and tolerance bounds to final_inspection_parameters
            migrationBuilder.Sql(@"
                ALTER TABLE final_inspection_parameters
                ADD COLUMN IF NOT EXISTS ""UoM"" VARCHAR(32) NOT NULL DEFAULT 'mm',
                ADD COLUMN IF NOT EXISTS ""TargetValue"" NUMERIC(14, 4) NULL,
                ADD COLUMN IF NOT EXISTS ""MinTolerance"" NUMERIC(14, 4) NULL,
                ADD COLUMN IF NOT EXISTS ""MaxTolerance"" NUMERIC(14, 4) NULL,
                ADD COLUMN IF NOT EXISTS ""ActualNumericValue"" NUMERIC(14, 4) NULL;
            ");

            // Index for incoming_inspection_samples
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_incoming_inspection_samples_IncomingChecklistItemId"" ON incoming_inspection_samples (""IncomingChecklistItemId"");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS incoming_inspection_samples CASCADE;");

            migrationBuilder.Sql(@"
                ALTER TABLE incoming_checklist_items
                DROP COLUMN IF EXISTS ""UoM"",
                DROP COLUMN IF EXISTS ""TargetValue"",
                DROP COLUMN IF EXISTS ""MinTolerance"",
                DROP COLUMN IF EXISTS ""MaxTolerance"",
                DROP COLUMN IF EXISTS ""MeanValue"",
                DROP COLUMN IF EXISTS ""StdDeviation"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE incoming_inspections
                DROP COLUMN IF EXISTS ""ReworkQuantity"",
                DROP COLUMN IF EXISTS ""ScrapQuantity"",
                DROP COLUMN IF EXISTS ""RtvQuantity"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE in_process_checks
                DROP COLUMN IF EXISTS ""UoM"",
                DROP COLUMN IF EXISTS ""TargetValue"",
                DROP COLUMN IF EXISTS ""MinTolerance"",
                DROP COLUMN IF EXISTS ""MaxTolerance"",
                DROP COLUMN IF EXISTS ""ActualNumericValue"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE final_inspection_parameters
                DROP COLUMN IF EXISTS ""UoM"",
                DROP COLUMN IF EXISTS ""TargetValue"",
                DROP COLUMN IF EXISTS ""MinTolerance"",
                DROP COLUMN IF EXISTS ""MaxTolerance"",
                DROP COLUMN IF EXISTS ""ActualNumericValue"";
            ");
        }
    }
}
