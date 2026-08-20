using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportDetailsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LorryReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LrNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DispatchId = table.Column<int>(type: "integer", nullable: false),
                    DispatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TransportId = table.Column<int>(type: "integer", nullable: false),
                    TransportNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VehicleNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LrDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Consignor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Consignee = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Packages = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    FreightCharges = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_LorryReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LrDocumentSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LrDocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransportDetailId = table.Column<int>(type: "integer", nullable: false),
                    AttachmentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SizeKb = table.Column<int>(type: "integer", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportAttachments_TransportDetails_TransportDetailId",
                        column: x => x.TransportDetailId,
                        principalTable: "TransportDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransportDetailId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_TransportTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportTimelineEvents_TransportDetails_TransportDetailId",
                        column: x => x.TransportDetailId,
                        principalTable: "TransportDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LorryReceipts_CustomerId",
                table: "LorryReceipts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LorryReceipts_DispatchId",
                table: "LorryReceipts",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_LorryReceipts_LrNumber",
                table: "LorryReceipts",
                column: "LrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LorryReceipts_Status",
                table: "LorryReceipts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LorryReceipts_TransportId",
                table: "LorryReceipts",
                column: "TransportId");

            migrationBuilder.CreateIndex(
                name: "IX_LrDocumentSequences_Prefix",
                table: "LrDocumentSequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportAttachments_TransportDetailId",
                table: "TransportAttachments",
                column: "TransportDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportTimelineEvents_TransportDetailId",
                table: "TransportTimelineEvents",
                column: "TransportDetailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LorryReceipts");

            migrationBuilder.DropTable(
                name: "LrDocumentSequences");

            migrationBuilder.DropTable(
                name: "TransportAttachments");

            migrationBuilder.DropTable(
                name: "TransportTimelineEvents");
        }
    }
}
