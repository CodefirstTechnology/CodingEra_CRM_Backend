using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <summary>
    /// Migration: AddGroup2ValidationTestingCertification
    /// Adds telemetry jsonb column to load_test_reports, and cryptographic hash, 
    /// verification QR URL, sales order linkage, dispatch plan linkage, and MTR heat number to test_certificates.
    /// </summary>
    [DbContext(typeof(ERPDbContext))]
    [Migration("20261001164000_AddGroup2ValidationTestingCertification")]
    public partial class AddGroup2ValidationTestingCertification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add TelemetryPointsJson (jsonb) to load_test_reports
            migrationBuilder.Sql(@"
                ALTER TABLE load_test_reports
                ADD COLUMN IF NOT EXISTS ""TelemetryPointsJson"" JSONB NULL;
            ");

            // 2. Add cryptographic verification & linkage columns to test_certificates
            migrationBuilder.Sql(@"
                ALTER TABLE test_certificates
                ADD COLUMN IF NOT EXISTS ""CertificateHash"" VARCHAR(64) NULL,
                ADD COLUMN IF NOT EXISTS ""VerificationQrUrl"" VARCHAR(512) NULL,
                ADD COLUMN IF NOT EXISTS ""SalesOrderId"" INT NULL,
                ADD COLUMN IF NOT EXISTS ""DispatchPlanId"" INT NULL,
                ADD COLUMN IF NOT EXISTS ""HeatNumber"" VARCHAR(64) NULL;
            ");

            // Indexes
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_test_certificates_SalesOrderId"" ON test_certificates (""SalesOrderId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_test_certificates_CertificateHash"" ON test_certificates (""CertificateHash"");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE load_test_reports
                DROP COLUMN IF EXISTS ""TelemetryPointsJson"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE test_certificates
                DROP COLUMN IF EXISTS ""CertificateHash"",
                DROP COLUMN IF EXISTS ""VerificationQrUrl"",
                DROP COLUMN IF EXISTS ""SalesOrderId"",
                DROP COLUMN IF EXISTS ""DispatchPlanId"",
                DROP COLUMN IF EXISTS ""HeatNumber"";
            ");
        }
    }
}
