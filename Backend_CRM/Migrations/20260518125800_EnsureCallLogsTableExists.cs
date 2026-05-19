using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class EnsureCallLogsTableExists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DBs that never applied early migrations may lack ""CallLogs""; add the full table idempotently.
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""CallLogs"" (
    ""CallId"" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ""Direction"" text NOT NULL DEFAULT '',
    ""PhoneNumber"" text NOT NULL DEFAULT '',
    ""ContactCompany"" text NOT NULL DEFAULT '',
    ""ContactName"" text NOT NULL DEFAULT '',
    ""CallStarted"" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    ""DurationMinutes"" integer NOT NULL DEFAULT 0,
    ""DurationSeconds"" integer NOT NULL DEFAULT 0,
    ""Outcome"" text NOT NULL DEFAULT '',
    ""CallSummary"" text NULL,
    ""ContactId"" integer NULL,
    ""RelatedLeadId"" integer NULL,
    ""RelatedDealId"" integer NULL,
    ""LastModified"" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    ""CreatedByUserId"" integer NULL,
    ""UpdatedByUserId"" integer NULL
);
");

            migrationBuilder.Sql(@"
ALTER TABLE ""CallLogs"" ADD COLUMN IF NOT EXISTS ""ContactId"" integer NULL;
ALTER TABLE ""CallLogs"" ADD COLUMN IF NOT EXISTS ""ContactName"" text NOT NULL DEFAULT '';
ALTER TABLE ""CallLogs"" ADD COLUMN IF NOT EXISTS ""LastModified"" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);
ALTER TABLE ""CallLogs"" ADD COLUMN IF NOT EXISTS ""RelatedDealId"" integer NULL;
ALTER TABLE ""CallLogs"" ADD COLUMN IF NOT EXISTS ""RelatedLeadId"" integer NULL;
ALTER TABLE ""CallLogs"" ADD COLUMN IF NOT EXISTS ""CreatedByUserId"" integer NULL;
ALTER TABLE ""CallLogs"" ADD COLUMN IF NOT EXISTS ""UpdatedByUserId"" integer NULL;
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ""IX_CallLogs_ContactId"" ON ""CallLogs"" (""ContactId"");
CREATE INDEX IF NOT EXISTS ""IX_CallLogs_RelatedDealId"" ON ""CallLogs"" (""RelatedDealId"");
CREATE INDEX IF NOT EXISTS ""IX_CallLogs_RelatedLeadId"" ON ""CallLogs"" (""RelatedLeadId"");
CREATE INDEX IF NOT EXISTS ""IX_CallLogs_CreatedByUserId"" ON ""CallLogs"" (""CreatedByUserId"");
CREATE INDEX IF NOT EXISTS ""IX_CallLogs_UpdatedByUserId"" ON ""CallLogs"" (""UpdatedByUserId"");
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_CallLogs_contacts_ContactId') THEN
    ALTER TABLE ""CallLogs"" ADD CONSTRAINT ""FK_CallLogs_contacts_ContactId""
      FOREIGN KEY (""ContactId"") REFERENCES contacts (id) ON DELETE SET NULL;
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_CallLogs_deals_RelatedDealId') THEN
    ALTER TABLE ""CallLogs"" ADD CONSTRAINT ""FK_CallLogs_deals_RelatedDealId""
      FOREIGN KEY (""RelatedDealId"") REFERENCES deals (id) ON DELETE SET NULL;
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_CallLogs_leads_RelatedLeadId') THEN
    ALTER TABLE ""CallLogs"" ADD CONSTRAINT ""FK_CallLogs_leads_RelatedLeadId""
      FOREIGN KEY (""RelatedLeadId"") REFERENCES leads (id) ON DELETE SET NULL;
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_CallLogs_users_CreatedByUserId') THEN
    ALTER TABLE ""CallLogs"" ADD CONSTRAINT ""FK_CallLogs_users_CreatedByUserId""
      FOREIGN KEY (""CreatedByUserId"") REFERENCES users (id) ON DELETE SET NULL;
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_CallLogs_users_UpdatedByUserId') THEN
    ALTER TABLE ""CallLogs"" ADD CONSTRAINT ""FK_CallLogs_users_UpdatedByUserId""
      FOREIGN KEY (""UpdatedByUserId"") REFERENCES users (id) ON DELETE SET NULL;
  END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Align-only migration; Down is not supported.");
        }
    }
}
