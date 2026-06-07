using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company_profiles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    brand_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    company_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    tagline = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    business_line = table.Column<string>(type: "text", nullable: false),
                    logo_content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    logo_base64 = table.Column<string>(type: "text", nullable: false),
                    gstin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cin_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    contact_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    website = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    account_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ifsc_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    branch_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    signatory_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    signatory_mobile = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    terms_conditions_json = table.Column<string>(type: "text", nullable: false),
                    intro_text = table.Column<string>(type: "text", nullable: false),
                    transportation_label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    jurisdiction = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    default_gst_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_profiles", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_profiles");
        }
    }
}
