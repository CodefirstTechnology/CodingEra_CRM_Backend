using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuotationApprovalDocumentSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinancialYear = table.Column<int>(type: "integer", nullable: false),
                    Prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LastNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationApprovalDocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuotationApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApprovalNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QuotationId = table.Column<int>(type: "integer", nullable: false),
                    QuotationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SalesOrderId = table.Column<int>(type: "integer", nullable: true),
                    SalesOrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SalesPersonUserId = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ApprovalLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationApprovals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuotationApprovalComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuotationApprovalId = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CommentedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CommentedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationApprovalComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationApprovalComments_QuotationApprovals_QuotationAppro~",
                        column: x => x.QuotationApprovalId,
                        principalTable: "QuotationApprovals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotationApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuotationApprovalId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PerformedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationApprovalHistories_QuotationApprovals_QuotationAppr~",
                        column: x => x.QuotationApprovalId,
                        principalTable: "QuotationApprovals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuotationApprovalComments_QuotationApprovalId",
                table: "QuotationApprovalComments",
                column: "QuotationApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationApprovalDocumentSequences_FinancialYear_Prefix",
                table: "QuotationApprovalDocumentSequences",
                columns: new[] { "FinancialYear", "Prefix" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotationApprovalHistories_QuotationApprovalId",
                table: "QuotationApprovalHistories",
                column: "QuotationApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationApprovals_ApprovalNumber",
                table: "QuotationApprovals",
                column: "ApprovalNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotationApprovals_QuotationNumber",
                table: "QuotationApprovals",
                column: "QuotationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationApprovals_RequestDate",
                table: "QuotationApprovals",
                column: "RequestDate");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationApprovals_SalesPersonUserId",
                table: "QuotationApprovals",
                column: "SalesPersonUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationApprovals_Status",
                table: "QuotationApprovals",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuotationApprovalComments");

            migrationBuilder.DropTable(
                name: "QuotationApprovalDocumentSequences");

            migrationBuilder.DropTable(
                name: "QuotationApprovalHistories");

            migrationBuilder.DropTable(
                name: "QuotationApprovals");
        }
    }
}
