using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "author_id",
                table: "notes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // Clear orphan FK values so constraints can be applied (existing dev data)
            migrationBuilder.Sql(@"
UPDATE notes SET author_id = NULL WHERE author_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM users u WHERE u.id = notes.author_id);
UPDATE notes SET related_lead_id = NULL WHERE related_lead_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM leads l WHERE l.id = notes.related_lead_id);
UPDATE notes SET related_deal_id = NULL WHERE related_deal_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM deals d WHERE d.id = notes.related_deal_id);
UPDATE notes SET related_contact_id = NULL WHERE related_contact_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM contacts c WHERE c.id = notes.related_contact_id);
UPDATE notes SET related_organization_id = NULL WHERE related_organization_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM organizations o WHERE o.id = notes.related_organization_id);
UPDATE ""Tasks"" SET ""AssigneeUserId"" = NULL WHERE ""AssigneeUserId"" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM users u WHERE u.id = ""Tasks"".""AssigneeUserId"");
UPDATE ""Tasks"" SET ""RelatedLeadId"" = NULL WHERE ""RelatedLeadId"" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM leads l WHERE l.id = ""Tasks"".""RelatedLeadId"");
UPDATE ""Tasks"" SET ""RelatedDealId"" = NULL WHERE ""RelatedDealId"" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM deals d WHERE d.id = ""Tasks"".""RelatedDealId"");
UPDATE ""CallLogs"" SET ""ContactId"" = NULL WHERE ""ContactId"" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM contacts c WHERE c.id = ""CallLogs"".""ContactId"");
UPDATE ""CallLogs"" SET ""RelatedLeadId"" = NULL WHERE ""RelatedLeadId"" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM leads l WHERE l.id = ""CallLogs"".""RelatedLeadId"");
UPDATE ""CallLogs"" SET ""RelatedDealId"" = NULL WHERE ""RelatedDealId"" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM deals d WHERE d.id = ""CallLogs"".""RelatedDealId"");
UPDATE contacts SET organization_id = NULL WHERE organization_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM organizations o WHERE o.id = contacts.organization_id);
UPDATE leads SET organization_id = NULL WHERE organization_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM organizations o WHERE o.id = leads.organization_id);
UPDATE leads SET lead_owner_id = NULL WHERE lead_owner_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM users u WHERE u.id = leads.lead_owner_id);
UPDATE deals SET organization_id = NULL WHERE organization_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM organizations o WHERE o.id = deals.organization_id);
UPDATE deals SET related_organization_id = NULL WHERE related_organization_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM organizations o WHERE o.id = deals.related_organization_id);
UPDATE deals SET contact_id = NULL WHERE contact_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM contacts c WHERE c.id = deals.contact_id);
UPDATE deals SET related_contact_id = NULL WHERE related_contact_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM contacts c WHERE c.id = deals.related_contact_id);
UPDATE deals SET deal_owner_id = NULL WHERE deal_owner_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM users u WHERE u.id = deals.deal_owner_id);
UPDATE deals SET assigned_to_user_id = NULL WHERE assigned_to_user_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM users u WHERE u.id = deals.assigned_to_user_id);
");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssigneeUserId",
                table: "Tasks",
                column: "AssigneeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_RelatedDealId",
                table: "Tasks",
                column: "RelatedDealId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_RelatedLeadId",
                table: "Tasks",
                column: "RelatedLeadId");

            migrationBuilder.CreateIndex(
                name: "IX_notes_author_id",
                table: "notes",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_notes_related_contact_id",
                table: "notes",
                column: "related_contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_notes_related_deal_id",
                table: "notes",
                column: "related_deal_id");

            migrationBuilder.CreateIndex(
                name: "IX_notes_related_lead_id",
                table: "notes",
                column: "related_lead_id");

            migrationBuilder.CreateIndex(
                name: "IX_notes_related_organization_id",
                table: "notes",
                column: "related_organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_leads_lead_owner_id",
                table: "leads",
                column: "lead_owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_leads_organization_id",
                table: "leads",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_deals_assigned_to_user_id",
                table: "deals",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_deals_contact_id",
                table: "deals",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_deals_deal_owner_id",
                table: "deals",
                column: "deal_owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_deals_organization_id",
                table: "deals",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_deals_related_contact_id",
                table: "deals",
                column: "related_contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_deals_related_organization_id",
                table: "deals",
                column: "related_organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_organization_id",
                table: "contacts",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_ContactId",
                table: "CallLogs",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_RelatedDealId",
                table: "CallLogs",
                column: "RelatedDealId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_RelatedLeadId",
                table: "CallLogs",
                column: "RelatedLeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_contacts_ContactId",
                table: "CallLogs",
                column: "ContactId",
                principalTable: "contacts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_deals_RelatedDealId",
                table: "CallLogs",
                column: "RelatedDealId",
                principalTable: "deals",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_leads_RelatedLeadId",
                table: "CallLogs",
                column: "RelatedLeadId",
                principalTable: "leads",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_contacts_organizations_organization_id",
                table: "contacts",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_contacts_contact_id",
                table: "deals",
                column: "contact_id",
                principalTable: "contacts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_contacts_related_contact_id",
                table: "deals",
                column: "related_contact_id",
                principalTable: "contacts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_organizations_organization_id",
                table: "deals",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_organizations_related_organization_id",
                table: "deals",
                column: "related_organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_users_assigned_to_user_id",
                table: "deals",
                column: "assigned_to_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_users_deal_owner_id",
                table: "deals",
                column: "deal_owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_organizations_organization_id",
                table: "leads",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_users_lead_owner_id",
                table: "leads",
                column: "lead_owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notes_contacts_related_contact_id",
                table: "notes",
                column: "related_contact_id",
                principalTable: "contacts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notes_deals_related_deal_id",
                table: "notes",
                column: "related_deal_id",
                principalTable: "deals",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notes_leads_related_lead_id",
                table: "notes",
                column: "related_lead_id",
                principalTable: "leads",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notes_organizations_related_organization_id",
                table: "notes",
                column: "related_organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notes_users_author_id",
                table: "notes",
                column: "author_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_deals_RelatedDealId",
                table: "Tasks",
                column: "RelatedDealId",
                principalTable: "deals",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_leads_RelatedLeadId",
                table: "Tasks",
                column: "RelatedLeadId",
                principalTable: "leads",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_users_AssigneeUserId",
                table: "Tasks",
                column: "AssigneeUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallLogs_contacts_ContactId",
                table: "CallLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CallLogs_deals_RelatedDealId",
                table: "CallLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CallLogs_leads_RelatedLeadId",
                table: "CallLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_contacts_organizations_organization_id",
                table: "contacts");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_contacts_contact_id",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_contacts_related_contact_id",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_organizations_organization_id",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_organizations_related_organization_id",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_users_assigned_to_user_id",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_users_deal_owner_id",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_organizations_organization_id",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_users_lead_owner_id",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "FK_notes_contacts_related_contact_id",
                table: "notes");

            migrationBuilder.DropForeignKey(
                name: "FK_notes_deals_related_deal_id",
                table: "notes");

            migrationBuilder.DropForeignKey(
                name: "FK_notes_leads_related_lead_id",
                table: "notes");

            migrationBuilder.DropForeignKey(
                name: "FK_notes_organizations_related_organization_id",
                table: "notes");

            migrationBuilder.DropForeignKey(
                name: "FK_notes_users_author_id",
                table: "notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_deals_RelatedDealId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_leads_RelatedLeadId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_users_AssigneeUserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_AssigneeUserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_RelatedDealId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_RelatedLeadId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_notes_author_id",
                table: "notes");

            migrationBuilder.DropIndex(
                name: "IX_notes_related_contact_id",
                table: "notes");

            migrationBuilder.DropIndex(
                name: "IX_notes_related_deal_id",
                table: "notes");

            migrationBuilder.DropIndex(
                name: "IX_notes_related_lead_id",
                table: "notes");

            migrationBuilder.DropIndex(
                name: "IX_notes_related_organization_id",
                table: "notes");

            migrationBuilder.DropIndex(
                name: "IX_leads_lead_owner_id",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_leads_organization_id",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_deals_assigned_to_user_id",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_deals_contact_id",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_deals_deal_owner_id",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_deals_organization_id",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_deals_related_contact_id",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_deals_related_organization_id",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_contacts_organization_id",
                table: "contacts");

            migrationBuilder.DropIndex(
                name: "IX_CallLogs_ContactId",
                table: "CallLogs");

            migrationBuilder.DropIndex(
                name: "IX_CallLogs_RelatedDealId",
                table: "CallLogs");

            migrationBuilder.DropIndex(
                name: "IX_CallLogs_RelatedLeadId",
                table: "CallLogs");

            migrationBuilder.AlterColumn<int>(
                name: "author_id",
                table: "notes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
