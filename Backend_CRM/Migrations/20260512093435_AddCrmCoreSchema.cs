using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmCoreSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssigneeUserId",
                table: "Tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "RelatedDealId",
                table: "Tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RelatedLeadId",
                table: "Tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "related_contact_id",
                table: "notes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "related_deal_id",
                table: "notes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "related_id",
                table: "notes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "related_lead_id",
                table: "notes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "related_name",
                table: "notes",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "related_organization_id",
                table: "notes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "related_type",
                table: "notes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "lead");

            migrationBuilder.AddColumn<string>(
                name: "visibility",
                table: "notes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "team");

            migrationBuilder.AddColumn<int>(
                name: "ContactId",
                table: "CallLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "CallLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "CallLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "RelatedDealId",
                table: "CallLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RelatedLeadId",
                table: "CallLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    salutation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    organization_id = table.Column<int>(type: "integer", nullable: true),
                    designation = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<int>(type: "integer", nullable: true),
                    contact_id = table.Column<int>(type: "integer", nullable: true),
                    organization_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    salutation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    mobile = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    annual_revenue = table.Column<decimal>(type: "numeric", nullable: true),
                    employees = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    website = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    territory = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    industry = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    deal_owner_id = table.Column<int>(type: "integer", nullable: true),
                    assigned_to_user_id = table.Column<int>(type: "integer", nullable: true),
                    assigned_initials = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    related_contact_id = table.Column<int>(type: "integer", nullable: true),
                    related_organization_id = table.Column<int>(type: "integer", nullable: true),
                    probability_percent = table.Column<int>(type: "integer", nullable: true),
                    next_step = table.Column<string>(type: "text", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    salutation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    mobile = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    organization = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    organization_id = table.Column<int>(type: "integer", nullable: true),
                    employees = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    annual_revenue = table.Column<decimal>(type: "numeric", nullable: true),
                    website = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    territory = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    industry = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    job_title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    request_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    lead_owner_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    owner = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    lead_owner_id = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lead_source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sort_timestamp = table.Column<long>(type: "bigint", nullable: true),
                    external_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    product = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    website = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    industry = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    annual_revenue = table.Column<decimal>(type: "numeric", nullable: true),
                    employees = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    territory = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leads_external_ref",
                table: "leads",
                column: "external_ref",
                unique: true,
                filter: "\"external_ref\" IS NOT NULL AND \"external_ref\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contacts");

            migrationBuilder.DropTable(
                name: "deals");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropColumn(
                name: "AssigneeUserId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RelatedDealId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RelatedLeadId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "related_contact_id",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "related_deal_id",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "related_id",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "related_lead_id",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "related_name",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "related_organization_id",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "related_type",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "visibility",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "CallLogs");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "CallLogs");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "CallLogs");

            migrationBuilder.DropColumn(
                name: "RelatedDealId",
                table: "CallLogs");

            migrationBuilder.DropColumn(
                name: "RelatedLeadId",
                table: "CallLogs");
        }
    }
}
