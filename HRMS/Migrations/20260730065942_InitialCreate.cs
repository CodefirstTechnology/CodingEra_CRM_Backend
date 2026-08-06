using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    department = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    designation = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    branch = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    joining_date = table.Column<DateOnly>(type: "date", nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    blood_group = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    account_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ifsc_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employees_department",
                table: "employees",
                column: "department");

            migrationBuilder.CreateIndex(
                name: "IX_employees_email",
                table: "employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_status",
                table: "employees",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employees");
        }
    }
}
