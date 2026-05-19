using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUpdatedByUserAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "territories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "territories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "salutations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "salutations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "request_types",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "request_types",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "notes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "notes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "leads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "leads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "lead_statuses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "lead_statuses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "lead_histories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "lead_histories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "industries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "industries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "employee_counts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "employee_counts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "deals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "deals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "crm_roles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "crm_roles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "created_by_user_id",
                table: "contacts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "updated_by_user_id",
                table: "contacts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_created_by_user_id",
                table: "users",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_updated_by_user_id",
                table: "users",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_territories_created_by_user_id",
                table: "territories",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_territories_updated_by_user_id",
                table: "territories",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedByUserId",
                table: "Tasks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UpdatedByUserId",
                table: "Tasks",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_salutations_created_by_user_id",
                table: "salutations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_salutations_updated_by_user_id",
                table: "salutations",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_types_created_by_user_id",
                table: "request_types",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_types_updated_by_user_id",
                table: "request_types",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_created_by_user_id",
                table: "organizations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_updated_by_user_id",
                table: "organizations",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notes_created_by_user_id",
                table: "notes",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notes_updated_by_user_id",
                table: "notes",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leads_created_by_user_id",
                table: "leads",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leads_updated_by_user_id",
                table: "leads",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_lead_statuses_created_by_user_id",
                table: "lead_statuses",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_lead_statuses_updated_by_user_id",
                table: "lead_statuses",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_lead_histories_created_by_user_id",
                table: "lead_histories",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_lead_histories_updated_by_user_id",
                table: "lead_histories",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_industries_created_by_user_id",
                table: "industries",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_industries_updated_by_user_id",
                table: "industries",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_counts_created_by_user_id",
                table: "employee_counts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_counts_updated_by_user_id",
                table: "employee_counts",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_deals_created_by_user_id",
                table: "deals",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_deals_updated_by_user_id",
                table: "deals",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_crm_roles_created_by_user_id",
                table: "crm_roles",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_crm_roles_updated_by_user_id",
                table: "crm_roles",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_created_by_user_id",
                table: "contacts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_updated_by_user_id",
                table: "contacts",
                column: "updated_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_contacts_users_created_by_user_id",
                table: "contacts",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_contacts_users_updated_by_user_id",
                table: "contacts",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_crm_roles_users_created_by_user_id",
                table: "crm_roles",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_crm_roles_users_updated_by_user_id",
                table: "crm_roles",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_users_created_by_user_id",
                table: "deals",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_users_updated_by_user_id",
                table: "deals",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_counts_users_created_by_user_id",
                table: "employee_counts",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_counts_users_updated_by_user_id",
                table: "employee_counts",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_industries_users_created_by_user_id",
                table: "industries",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_industries_users_updated_by_user_id",
                table: "industries",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_lead_histories_users_created_by_user_id",
                table: "lead_histories",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_lead_histories_users_updated_by_user_id",
                table: "lead_histories",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_lead_statuses_users_created_by_user_id",
                table: "lead_statuses",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_lead_statuses_users_updated_by_user_id",
                table: "lead_statuses",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_users_created_by_user_id",
                table: "leads",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_users_updated_by_user_id",
                table: "leads",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notes_users_created_by_user_id",
                table: "notes",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notes_users_updated_by_user_id",
                table: "notes",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_organizations_users_created_by_user_id",
                table: "organizations",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_organizations_users_updated_by_user_id",
                table: "organizations",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_request_types_users_created_by_user_id",
                table: "request_types",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_request_types_users_updated_by_user_id",
                table: "request_types",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_salutations_users_created_by_user_id",
                table: "salutations",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_salutations_users_updated_by_user_id",
                table: "salutations",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_users_CreatedByUserId",
                table: "Tasks",
                column: "CreatedByUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_users_UpdatedByUserId",
                table: "Tasks",
                column: "UpdatedByUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_territories_users_created_by_user_id",
                table: "territories",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_territories_users_updated_by_user_id",
                table: "territories",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_created_by_user_id",
                table: "users",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_updated_by_user_id",
                table: "users",
                column: "updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contacts_users_created_by_user_id",
                table: "contacts");

            migrationBuilder.DropForeignKey(
                name: "FK_contacts_users_updated_by_user_id",
                table: "contacts");

            migrationBuilder.DropForeignKey(
                name: "FK_crm_roles_users_created_by_user_id",
                table: "crm_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_crm_roles_users_updated_by_user_id",
                table: "crm_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_users_created_by_user_id",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_users_updated_by_user_id",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_counts_users_created_by_user_id",
                table: "employee_counts");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_counts_users_updated_by_user_id",
                table: "employee_counts");

            migrationBuilder.DropForeignKey(
                name: "FK_industries_users_created_by_user_id",
                table: "industries");

            migrationBuilder.DropForeignKey(
                name: "FK_industries_users_updated_by_user_id",
                table: "industries");

            migrationBuilder.DropForeignKey(
                name: "FK_lead_histories_users_created_by_user_id",
                table: "lead_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_lead_histories_users_updated_by_user_id",
                table: "lead_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_lead_statuses_users_created_by_user_id",
                table: "lead_statuses");

            migrationBuilder.DropForeignKey(
                name: "FK_lead_statuses_users_updated_by_user_id",
                table: "lead_statuses");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_users_created_by_user_id",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_users_updated_by_user_id",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "FK_notes_users_created_by_user_id",
                table: "notes");

            migrationBuilder.DropForeignKey(
                name: "FK_notes_users_updated_by_user_id",
                table: "notes");

            migrationBuilder.DropForeignKey(
                name: "FK_organizations_users_created_by_user_id",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_organizations_users_updated_by_user_id",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_request_types_users_created_by_user_id",
                table: "request_types");

            migrationBuilder.DropForeignKey(
                name: "FK_request_types_users_updated_by_user_id",
                table: "request_types");

            migrationBuilder.DropForeignKey(
                name: "FK_salutations_users_created_by_user_id",
                table: "salutations");

            migrationBuilder.DropForeignKey(
                name: "FK_salutations_users_updated_by_user_id",
                table: "salutations");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_users_CreatedByUserId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_users_UpdatedByUserId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_territories_users_created_by_user_id",
                table: "territories");

            migrationBuilder.DropForeignKey(
                name: "FK_territories_users_updated_by_user_id",
                table: "territories");

            migrationBuilder.DropForeignKey(
                name: "FK_users_users_created_by_user_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_users_updated_by_user_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_created_by_user_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_updated_by_user_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_territories_created_by_user_id",
                table: "territories");

            migrationBuilder.DropIndex(
                name: "IX_territories_updated_by_user_id",
                table: "territories");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_CreatedByUserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_UpdatedByUserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_salutations_created_by_user_id",
                table: "salutations");

            migrationBuilder.DropIndex(
                name: "IX_salutations_updated_by_user_id",
                table: "salutations");

            migrationBuilder.DropIndex(
                name: "IX_request_types_created_by_user_id",
                table: "request_types");

            migrationBuilder.DropIndex(
                name: "IX_request_types_updated_by_user_id",
                table: "request_types");

            migrationBuilder.DropIndex(
                name: "IX_organizations_created_by_user_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_organizations_updated_by_user_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_notes_created_by_user_id",
                table: "notes");

            migrationBuilder.DropIndex(
                name: "IX_notes_updated_by_user_id",
                table: "notes");

            migrationBuilder.DropIndex(
                name: "IX_leads_created_by_user_id",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_leads_updated_by_user_id",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_lead_statuses_created_by_user_id",
                table: "lead_statuses");

            migrationBuilder.DropIndex(
                name: "IX_lead_statuses_updated_by_user_id",
                table: "lead_statuses");

            migrationBuilder.DropIndex(
                name: "IX_lead_histories_created_by_user_id",
                table: "lead_histories");

            migrationBuilder.DropIndex(
                name: "IX_lead_histories_updated_by_user_id",
                table: "lead_histories");

            migrationBuilder.DropIndex(
                name: "IX_industries_created_by_user_id",
                table: "industries");

            migrationBuilder.DropIndex(
                name: "IX_industries_updated_by_user_id",
                table: "industries");

            migrationBuilder.DropIndex(
                name: "IX_employee_counts_created_by_user_id",
                table: "employee_counts");

            migrationBuilder.DropIndex(
                name: "IX_employee_counts_updated_by_user_id",
                table: "employee_counts");

            migrationBuilder.DropIndex(
                name: "IX_deals_created_by_user_id",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_deals_updated_by_user_id",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_crm_roles_created_by_user_id",
                table: "crm_roles");

            migrationBuilder.DropIndex(
                name: "IX_crm_roles_updated_by_user_id",
                table: "crm_roles");

            migrationBuilder.DropIndex(
                name: "IX_contacts_created_by_user_id",
                table: "contacts");

            migrationBuilder.DropIndex(
                name: "IX_contacts_updated_by_user_id",
                table: "contacts");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "territories");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "territories");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "salutations");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "salutations");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "request_types");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "request_types");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "lead_statuses");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "lead_statuses");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "lead_histories");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "lead_histories");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "industries");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "industries");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "employee_counts");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "employee_counts");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "crm_roles");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "crm_roles");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "contacts");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "contacts");
        }
    }
}
