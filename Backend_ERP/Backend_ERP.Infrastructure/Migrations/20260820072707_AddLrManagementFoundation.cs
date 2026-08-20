using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLrManagementFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EwayBills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EwayBillNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DispatchId = table.Column<int>(type: "integer", nullable: false),
                    DispatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    GstNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VehicleNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TransportId = table.Column<int>(type: "integer", nullable: false),
                    TransportNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ValidityFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidityTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EwayBills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EwayDocumentSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EwayDocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LrAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LorryReceiptId = table.Column<int>(type: "integer", nullable: false),
                    AttachmentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SizeKb = table.Column<int>(type: "integer", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LrAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LrAttachments_LorryReceipts_LorryReceiptId",
                        column: x => x.LorryReceiptId,
                        principalTable: "LorryReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LrTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LorryReceiptId = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    User = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LrTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LrTimelineEvents_LorryReceipts_LorryReceiptId",
                        column: x => x.LorryReceiptId,
                        principalTable: "LorryReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EwayBills_CustomerId",
                table: "EwayBills",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_EwayBills_DispatchId",
                table: "EwayBills",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_EwayBills_EwayBillNumber",
                table: "EwayBills",
                column: "EwayBillNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EwayBills_Status",
                table: "EwayBills",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EwayBills_TransportId",
                table: "EwayBills",
                column: "TransportId");

            migrationBuilder.CreateIndex(
                name: "IX_EwayDocumentSequences_Prefix",
                table: "EwayDocumentSequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LrAttachments_LorryReceiptId",
                table: "LrAttachments",
                column: "LorryReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_LrTimelineEvents_LorryReceiptId",
                table: "LrTimelineEvents",
                column: "LorryReceiptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EwayBills");

            migrationBuilder.DropTable(
                name: "EwayDocumentSequences");

            migrationBuilder.DropTable(
                name: "LrAttachments");

            migrationBuilder.DropTable(
                name: "LrTimelineEvents");
        }
    }
}
