using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class UniversalStandardAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallLogs_users_CreatedByUserId",
                table: "CallLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CallLogs_users_UpdatedByUserId",
                table: "CallLogs");

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

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "users",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "users",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_users_updated_by_user_id",
                table: "users",
                newName: "IX_users_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_users_created_by_user_id",
                table: "users",
                newName: "IX_users_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "territories",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "territories",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_territories_updated_by_user_id",
                table: "territories",
                newName: "IX_territories_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_territories_created_by_user_id",
                table: "territories",
                newName: "IX_territories_created_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedByUserId",
                table: "Tasks",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "Tasks",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_UpdatedByUserId",
                table: "Tasks",
                newName: "IX_Tasks_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_CreatedByUserId",
                table: "Tasks",
                newName: "IX_Tasks_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "salutations",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "salutations",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_salutations_updated_by_user_id",
                table: "salutations",
                newName: "IX_salutations_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_salutations_created_by_user_id",
                table: "salutations",
                newName: "IX_salutations_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "request_types",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "request_types",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_request_types_updated_by_user_id",
                table: "request_types",
                newName: "IX_request_types_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_request_types_created_by_user_id",
                table: "request_types",
                newName: "IX_request_types_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "organizations",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "organizations",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_organizations_updated_by_user_id",
                table: "organizations",
                newName: "IX_organizations_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_organizations_created_by_user_id",
                table: "organizations",
                newName: "IX_organizations_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "notes",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "notes",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_notes_updated_by_user_id",
                table: "notes",
                newName: "IX_notes_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_notes_created_by_user_id",
                table: "notes",
                newName: "IX_notes_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "leads",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "leads",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_leads_updated_by_user_id",
                table: "leads",
                newName: "IX_leads_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_leads_created_by_user_id",
                table: "leads",
                newName: "IX_leads_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "lead_statuses",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "lead_statuses",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_lead_statuses_updated_by_user_id",
                table: "lead_statuses",
                newName: "IX_lead_statuses_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_lead_statuses_created_by_user_id",
                table: "lead_statuses",
                newName: "IX_lead_statuses_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "lead_histories",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "lead_histories",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_lead_histories_updated_by_user_id",
                table: "lead_histories",
                newName: "IX_lead_histories_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_lead_histories_created_by_user_id",
                table: "lead_histories",
                newName: "IX_lead_histories_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "industries",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "industries",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_industries_updated_by_user_id",
                table: "industries",
                newName: "IX_industries_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_industries_created_by_user_id",
                table: "industries",
                newName: "IX_industries_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "employee_counts",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "employee_counts",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_employee_counts_updated_by_user_id",
                table: "employee_counts",
                newName: "IX_employee_counts_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_employee_counts_created_by_user_id",
                table: "employee_counts",
                newName: "IX_employee_counts_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "deals",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "deals",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_deals_updated_by_user_id",
                table: "deals",
                newName: "IX_deals_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_deals_created_by_user_id",
                table: "deals",
                newName: "IX_deals_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "crm_roles",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "crm_roles",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_crm_roles_updated_by_user_id",
                table: "crm_roles",
                newName: "IX_crm_roles_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_crm_roles_created_by_user_id",
                table: "crm_roles",
                newName: "IX_crm_roles_created_by");

            migrationBuilder.RenameColumn(
                name: "updated_by_user_id",
                table: "contacts",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "contacts",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_contacts_updated_by_user_id",
                table: "contacts",
                newName: "IX_contacts_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_contacts_created_by_user_id",
                table: "contacts",
                newName: "IX_contacts_created_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedByUserId",
                table: "CallLogs",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "CallLogs",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_CallLogs_UpdatedByUserId",
                table: "CallLogs",
                newName: "IX_CallLogs_updated_by");

            migrationBuilder.RenameIndex(
                name: "IX_CallLogs_CreatedByUserId",
                table: "CallLogs",
                newName: "IX_CallLogs_created_by");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "territories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "territories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "Tasks",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "salutations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "salutations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "request_types",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "request_types",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "notes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "leads",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "lead_statuses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "lead_statuses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "lead_histories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "industries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "industries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "employee_counts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "employee_counts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "deals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "deals",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "deals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "crm_roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "crm_roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "contacts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "contacts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "contacts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "CallLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "CallLogs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "CallLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_users_created_by",
                table: "CallLogs",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_users_updated_by",
                table: "CallLogs",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_contacts_users_created_by",
                table: "contacts",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_contacts_users_updated_by",
                table: "contacts",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_crm_roles_users_created_by",
                table: "crm_roles",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_crm_roles_users_updated_by",
                table: "crm_roles",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_users_created_by",
                table: "deals",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_users_updated_by",
                table: "deals",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_counts_users_created_by",
                table: "employee_counts",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_counts_users_updated_by",
                table: "employee_counts",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_industries_users_created_by",
                table: "industries",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_industries_users_updated_by",
                table: "industries",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_lead_histories_users_created_by",
                table: "lead_histories",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_lead_histories_users_updated_by",
                table: "lead_histories",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_lead_statuses_users_created_by",
                table: "lead_statuses",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_lead_statuses_users_updated_by",
                table: "lead_statuses",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_users_created_by",
                table: "leads",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_users_updated_by",
                table: "leads",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notes_users_created_by",
                table: "notes",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notes_users_updated_by",
                table: "notes",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_organizations_users_created_by",
                table: "organizations",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_organizations_users_updated_by",
                table: "organizations",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_request_types_users_created_by",
                table: "request_types",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_request_types_users_updated_by",
                table: "request_types",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_salutations_users_created_by",
                table: "salutations",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_salutations_users_updated_by",
                table: "salutations",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_users_created_by",
                table: "Tasks",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_users_updated_by",
                table: "Tasks",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_territories_users_created_by",
                table: "territories",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_territories_users_updated_by",
                table: "territories",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_created_by",
                table: "users",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_updated_by",
                table: "users",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                """
                UPDATE users SET updated_at = created_at WHERE updated_at < '1970-01-02'::timestamptz;
                UPDATE organizations SET created_at = last_modified, updated_at = last_modified;
                UPDATE contacts SET created_at = last_modified, updated_at = last_modified;
                UPDATE deals SET created_at = last_modified, updated_at = last_modified;
                UPDATE salutations SET created_at = last_modified, updated_at = last_modified;
                UPDATE employee_counts SET created_at = last_modified, updated_at = last_modified;
                UPDATE territories SET created_at = last_modified, updated_at = last_modified;
                UPDATE industries SET created_at = last_modified, updated_at = last_modified;
                UPDATE lead_statuses SET created_at = last_modified, updated_at = last_modified;
                UPDATE request_types SET created_at = last_modified, updated_at = last_modified;
                UPDATE crm_roles SET created_at = last_modified, updated_at = last_modified;
                UPDATE "Tasks" SET created_at = "LastModified", updated_at = "LastModified";
                UPDATE "CallLogs" SET created_at = "LastModified", updated_at = "LastModified";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallLogs_users_created_by",
                table: "CallLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CallLogs_users_updated_by",
                table: "CallLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_contacts_users_created_by",
                table: "contacts");

            migrationBuilder.DropForeignKey(
                name: "FK_contacts_users_updated_by",
                table: "contacts");

            migrationBuilder.DropForeignKey(
                name: "FK_crm_roles_users_created_by",
                table: "crm_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_crm_roles_users_updated_by",
                table: "crm_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_users_created_by",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_users_updated_by",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_counts_users_created_by",
                table: "employee_counts");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_counts_users_updated_by",
                table: "employee_counts");

            migrationBuilder.DropForeignKey(
                name: "FK_industries_users_created_by",
                table: "industries");

            migrationBuilder.DropForeignKey(
                name: "FK_industries_users_updated_by",
                table: "industries");

            migrationBuilder.DropForeignKey(
                name: "FK_lead_histories_users_created_by",
                table: "lead_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_lead_histories_users_updated_by",
                table: "lead_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_lead_statuses_users_created_by",
                table: "lead_statuses");

            migrationBuilder.DropForeignKey(
                name: "FK_lead_statuses_users_updated_by",
                table: "lead_statuses");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_users_created_by",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_users_updated_by",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "FK_notes_users_created_by",
                table: "notes");

            migrationBuilder.DropForeignKey(
                name: "FK_notes_users_updated_by",
                table: "notes");

            migrationBuilder.DropForeignKey(
                name: "FK_organizations_users_created_by",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_organizations_users_updated_by",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_request_types_users_created_by",
                table: "request_types");

            migrationBuilder.DropForeignKey(
                name: "FK_request_types_users_updated_by",
                table: "request_types");

            migrationBuilder.DropForeignKey(
                name: "FK_salutations_users_created_by",
                table: "salutations");

            migrationBuilder.DropForeignKey(
                name: "FK_salutations_users_updated_by",
                table: "salutations");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_users_created_by",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_users_updated_by",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_territories_users_created_by",
                table: "territories");

            migrationBuilder.DropForeignKey(
                name: "FK_territories_users_updated_by",
                table: "territories");

            migrationBuilder.DropForeignKey(
                name: "FK_users_users_created_by",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_users_updated_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "territories");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "territories");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "salutations");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "salutations");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "request_types");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "request_types");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "lead_statuses");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "lead_statuses");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "lead_histories");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "industries");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "industries");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "employee_counts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "employee_counts");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "crm_roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "crm_roles");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "contacts");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "contacts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "contacts");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "CallLogs");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "CallLogs");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "CallLogs");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "users",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "users",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_users_updated_by",
                table: "users",
                newName: "IX_users_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_users_created_by",
                table: "users",
                newName: "IX_users_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "territories",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "territories",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_territories_updated_by",
                table: "territories",
                newName: "IX_territories_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_territories_created_by",
                table: "territories",
                newName: "IX_territories_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "Tasks",
                newName: "UpdatedByUserId");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Tasks",
                newName: "CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_updated_by",
                table: "Tasks",
                newName: "IX_Tasks_UpdatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_created_by",
                table: "Tasks",
                newName: "IX_Tasks_CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "salutations",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "salutations",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_salutations_updated_by",
                table: "salutations",
                newName: "IX_salutations_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_salutations_created_by",
                table: "salutations",
                newName: "IX_salutations_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "request_types",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "request_types",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_request_types_updated_by",
                table: "request_types",
                newName: "IX_request_types_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_request_types_created_by",
                table: "request_types",
                newName: "IX_request_types_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "organizations",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "organizations",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_organizations_updated_by",
                table: "organizations",
                newName: "IX_organizations_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_organizations_created_by",
                table: "organizations",
                newName: "IX_organizations_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "notes",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "notes",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_notes_updated_by",
                table: "notes",
                newName: "IX_notes_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_notes_created_by",
                table: "notes",
                newName: "IX_notes_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "leads",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "leads",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_leads_updated_by",
                table: "leads",
                newName: "IX_leads_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_leads_created_by",
                table: "leads",
                newName: "IX_leads_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "lead_statuses",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "lead_statuses",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_lead_statuses_updated_by",
                table: "lead_statuses",
                newName: "IX_lead_statuses_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_lead_statuses_created_by",
                table: "lead_statuses",
                newName: "IX_lead_statuses_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "lead_histories",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "lead_histories",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_lead_histories_updated_by",
                table: "lead_histories",
                newName: "IX_lead_histories_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_lead_histories_created_by",
                table: "lead_histories",
                newName: "IX_lead_histories_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "industries",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "industries",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_industries_updated_by",
                table: "industries",
                newName: "IX_industries_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_industries_created_by",
                table: "industries",
                newName: "IX_industries_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "employee_counts",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "employee_counts",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_employee_counts_updated_by",
                table: "employee_counts",
                newName: "IX_employee_counts_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_employee_counts_created_by",
                table: "employee_counts",
                newName: "IX_employee_counts_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "deals",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "deals",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_deals_updated_by",
                table: "deals",
                newName: "IX_deals_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_deals_created_by",
                table: "deals",
                newName: "IX_deals_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "crm_roles",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "crm_roles",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_crm_roles_updated_by",
                table: "crm_roles",
                newName: "IX_crm_roles_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_crm_roles_created_by",
                table: "crm_roles",
                newName: "IX_crm_roles_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "contacts",
                newName: "updated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "contacts",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_contacts_updated_by",
                table: "contacts",
                newName: "IX_contacts_updated_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_contacts_created_by",
                table: "contacts",
                newName: "IX_contacts_created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "CallLogs",
                newName: "UpdatedByUserId");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "CallLogs",
                newName: "CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_CallLogs_updated_by",
                table: "CallLogs",
                newName: "IX_CallLogs_UpdatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_CallLogs_created_by",
                table: "CallLogs",
                newName: "IX_CallLogs_CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_users_CreatedByUserId",
                table: "CallLogs",
                column: "CreatedByUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_users_UpdatedByUserId",
                table: "CallLogs",
                column: "UpdatedByUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

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
    }
}
