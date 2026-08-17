using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class HrmsFullSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "designations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_designations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leave_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    default_allocated_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_types", x => x.id);
                });

            migrationBuilder.Sql("""
                INSERT INTO branches (id, name, is_active) VALUES
                (1, 'Headquarters - Mumbai', TRUE),
                (2, 'Branch - Pune', TRUE),
                (3, 'Branch - Bangalore', TRUE)
                ON CONFLICT DO NOTHING;

                INSERT INTO departments (id, name, is_active) VALUES
                (1, 'Engineering', TRUE),
                (2, 'Human Resources', TRUE),
                (3, 'Sales', TRUE)
                ON CONFLICT DO NOTHING;

                INSERT INTO designations (id, name, is_active) VALUES
                (1, 'Senior Full Stack Developer', TRUE),
                (2, 'HR Lead', TRUE),
                (3, 'Sales Executive', TRUE)
                ON CONFLICT DO NOTHING;

                INSERT INTO document_categories (id, name, is_active) VALUES
                (1, 'Identity (Aadhaar)', TRUE),
                (2, 'PAN', TRUE),
                (3, 'Education', TRUE)
                ON CONFLICT DO NOTHING;

                INSERT INTO leave_types (id, name, code, default_allocated_days, is_active) VALUES
                (1, 'Casual Leave', 'CL', 12, TRUE),
                (2, 'Sick Leave', 'SL', 10, TRUE),
                (3, 'Earned Leave', 'EL', 15, TRUE)
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.DropIndex(
                name: "IX_employees_department",
                table: "employees");

            migrationBuilder.AddColumn<string>(
                name: "employee_code",
                table: "employees",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "department_id",
                table: "employees",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "designation_id",
                table: "employees",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "branch_id",
                table: "employees",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE employees
                SET employee_code = 'EMP-' || LPAD(id::text, 4, '0')
                WHERE employee_code = '';
                """);

            migrationBuilder.DropColumn(
                name: "branch",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "department",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "designation",
                table: "employees");

            migrationBuilder.CreateIndex(
                name: "IX_employees_branch_id",
                table: "employees",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_department_id",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_designation_id",
                table: "employees",
                column: "designation_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_employee_code",
                table: "employees",
                column: "employee_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branches_name",
                table: "branches",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_name",
                table: "departments",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_designations_name",
                table: "designations",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_categories_name",
                table: "document_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_types_code",
                table: "leave_types",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_branches_branch_id",
                table: "employees",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_departments_department_id",
                table: "employees",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_designations_designation_id",
                table: "employees",
                column: "designation_id",
                principalTable: "designations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateTable(
                name: "attendance_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    attendance_date = table.Column<DateOnly>(type: "date", nullable: false),
                    check_in = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    check_out = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    working_minutes = table.Column<int>(type: "integer", nullable: true),
                    overtime_minutes = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_attendance_records_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_documents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    document_category_id = table.Column<int>(type: "integer", nullable: false),
                    document_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    file_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_documents_document_categories_document_category_id",
                        column: x => x.document_category_id,
                        principalTable: "document_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_documents_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leave_allocations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    leave_type_id = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    allocated_days = table.Column<int>(type: "integer", nullable: false),
                    used_days = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_allocations", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_allocations_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_allocations_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    leave_type_id = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_days = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_requests_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_requests_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payroll_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    pay_month = table.Column<int>(type: "integer", nullable: false),
                    pay_year = table.Column<int>(type: "integer", nullable: false),
                    basic_salary = table.Column<decimal>(type: "numeric", nullable: false),
                    allowances = table.Column<decimal>(type: "numeric", nullable: false),
                    deductions = table.Column<decimal>(type: "numeric", nullable: false),
                    net_salary = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_payroll_records_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "performance_reviews",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    review_period = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    key_achievements = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    manager_rating = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performance_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_performance_reviews_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_attendance_date",
                table: "attendance_records",
                column: "attendance_date");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_employee_id_attendance_date",
                table: "attendance_records",
                columns: new[] { "employee_id", "attendance_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_status",
                table: "attendance_records",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_employee_documents_document_category_id",
                table: "employee_documents",
                column: "document_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_documents_employee_id",
                table: "employee_documents",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_allocations_employee_id_leave_type_id_year",
                table: "leave_allocations",
                columns: new[] { "employee_id", "leave_type_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_allocations_leave_type_id",
                table: "leave_allocations",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_employee_id",
                table: "leave_requests",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_leave_type_id",
                table: "leave_requests",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_status",
                table: "leave_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_records_employee_id_pay_month_pay_year",
                table: "payroll_records",
                columns: new[] { "employee_id", "pay_month", "pay_year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_records_status",
                table: "payroll_records",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_performance_reviews_employee_id_review_period",
                table: "performance_reviews",
                columns: new[] { "employee_id", "review_period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_performance_reviews_status",
                table: "performance_reviews",
                column: "status");

            migrationBuilder.Sql("""
                SELECT setval(pg_get_serial_sequence('departments', 'id'), (SELECT MAX(id) FROM departments));
                SELECT setval(pg_get_serial_sequence('branches', 'id'), (SELECT MAX(id) FROM branches));
                SELECT setval(pg_get_serial_sequence('designations', 'id'), (SELECT MAX(id) FROM designations));
                SELECT setval(pg_get_serial_sequence('leave_types', 'id'), (SELECT MAX(id) FROM leave_types));
                SELECT setval(pg_get_serial_sequence('document_categories', 'id'), (SELECT MAX(id) FROM document_categories));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "attendance_records");
            migrationBuilder.DropTable(name: "employee_documents");
            migrationBuilder.DropTable(name: "leave_allocations");
            migrationBuilder.DropTable(name: "leave_requests");
            migrationBuilder.DropTable(name: "payroll_records");
            migrationBuilder.DropTable(name: "performance_reviews");

            migrationBuilder.DropForeignKey(name: "FK_employees_branches_branch_id", table: "employees");
            migrationBuilder.DropForeignKey(name: "FK_employees_departments_department_id", table: "employees");
            migrationBuilder.DropForeignKey(name: "FK_employees_designations_designation_id", table: "employees");

            migrationBuilder.DropIndex(name: "IX_employees_branch_id", table: "employees");
            migrationBuilder.DropIndex(name: "IX_employees_department_id", table: "employees");
            migrationBuilder.DropIndex(name: "IX_employees_designation_id", table: "employees");
            migrationBuilder.DropIndex(name: "IX_employees_employee_code", table: "employees");

            migrationBuilder.DropColumn(name: "employee_code", table: "employees");
            migrationBuilder.DropColumn(name: "department_id", table: "employees");
            migrationBuilder.DropColumn(name: "designation_id", table: "employees");
            migrationBuilder.DropColumn(name: "branch_id", table: "employees");

            migrationBuilder.AddColumn<string>(
                name: "branch",
                table: "employees",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "department",
                table: "employees",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "designation",
                table: "employees",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_employees_department",
                table: "employees",
                column: "department");

            migrationBuilder.DropTable(name: "document_categories");
            migrationBuilder.DropTable(name: "leave_types");
            migrationBuilder.DropTable(name: "designations");
            migrationBuilder.DropTable(name: "branches");
            migrationBuilder.DropTable(name: "departments");
        }
    }
}
