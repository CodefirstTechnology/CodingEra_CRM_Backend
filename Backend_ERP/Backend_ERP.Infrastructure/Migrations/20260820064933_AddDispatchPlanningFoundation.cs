using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchPlanningFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DispatchDocumentSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchDocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DispatchPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DispatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DispatchDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SalesOrderId = table.Column<int>(type: "integer", nullable: false),
                    SalesOrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DeliveryAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    WarehouseName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PlannedDispatchDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VehicleRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VehicleAssignmentId = table.Column<int>(type: "integer", nullable: true),
                    TransportId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DispatchPlanAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DispatchPlanId = table.Column<int>(type: "integer", nullable: false),
                    AttachmentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SizeKb = table.Column<int>(type: "integer", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchPlanAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchPlanAttachments_DispatchPlans_DispatchPlanId",
                        column: x => x.DispatchPlanId,
                        principalTable: "DispatchPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchPlanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DispatchPlanId = table.Column<int>(type: "integer", nullable: false),
                    FinishedGoodId = table.Column<int>(type: "integer", nullable: false),
                    FinishedGoodCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinishedGoodName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Uom = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    WarehouseName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FinalInspectionId = table.Column<int>(type: "integer", nullable: true),
                    FinalInspectionNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TestCertificateId = table.Column<int>(type: "integer", nullable: true),
                    TestCertificateNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchPlanItems_DispatchPlans_DispatchPlanId",
                        column: x => x.DispatchPlanId,
                        principalTable: "DispatchPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchPlanTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DispatchPlanId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_DispatchPlanTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchPlanTimelineEvents_DispatchPlans_DispatchPlanId",
                        column: x => x.DispatchPlanId,
                        principalTable: "DispatchPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchDocumentSequences_Prefix",
                table: "DispatchDocumentSequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPlanAttachments_DispatchPlanId",
                table: "DispatchPlanAttachments",
                column: "DispatchPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPlanItems_DispatchPlanId",
                table: "DispatchPlanItems",
                column: "DispatchPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPlanItems_FinishedGoodId",
                table: "DispatchPlanItems",
                column: "FinishedGoodId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPlans_CustomerId",
                table: "DispatchPlans",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPlans_DispatchDate",
                table: "DispatchPlans",
                column: "DispatchDate");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPlans_DispatchNumber",
                table: "DispatchPlans",
                column: "DispatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPlans_SalesOrderId",
                table: "DispatchPlans",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPlans_Status",
                table: "DispatchPlans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPlans_WarehouseId",
                table: "DispatchPlans",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPlanTimelineEvents_DispatchPlanId",
                table: "DispatchPlanTimelineEvents",
                column: "DispatchPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispatchDocumentSequences");

            migrationBuilder.DropTable(
                name: "DispatchPlanAttachments");

            migrationBuilder.DropTable(
                name: "DispatchPlanItems");

            migrationBuilder.DropTable(
                name: "DispatchPlanTimelineEvents");

            migrationBuilder.DropTable(
                name: "DispatchPlans");
        }
    }
}
