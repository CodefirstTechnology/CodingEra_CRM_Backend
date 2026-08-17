using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_documents_document_categories_document_category_id",
                table: "employee_documents");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_branches_branch_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_departments_department_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_designations_designation_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "FK_leave_allocations_leave_types_leave_type_id",
                table: "leave_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_leave_requests_leave_types_leave_type_id",
                table: "leave_requests");

            migrationBuilder.AddForeignKey(
                name: "FK_employee_documents_document_categories_document_category_id",
                table: "employee_documents",
                column: "document_category_id",
                principalTable: "document_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_branches_branch_id",
                table: "employees",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_departments_department_id",
                table: "employees",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_designations_designation_id",
                table: "employees",
                column: "designation_id",
                principalTable: "designations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_leave_allocations_leave_types_leave_type_id",
                table: "leave_allocations",
                column: "leave_type_id",
                principalTable: "leave_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_leave_requests_leave_types_leave_type_id",
                table: "leave_requests",
                column: "leave_type_id",
                principalTable: "leave_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_documents_document_categories_document_category_id",
                table: "employee_documents");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_branches_branch_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_departments_department_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_designations_designation_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "FK_leave_allocations_leave_types_leave_type_id",
                table: "leave_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_leave_requests_leave_types_leave_type_id",
                table: "leave_requests");

            migrationBuilder.AddForeignKey(
                name: "FK_employee_documents_document_categories_document_category_id",
                table: "employee_documents",
                column: "document_category_id",
                principalTable: "document_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

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

            migrationBuilder.AddForeignKey(
                name: "FK_leave_allocations_leave_types_leave_type_id",
                table: "leave_allocations",
                column: "leave_type_id",
                principalTable: "leave_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_leave_requests_leave_types_leave_type_id",
                table: "leave_requests",
                column: "leave_type_id",
                principalTable: "leave_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
