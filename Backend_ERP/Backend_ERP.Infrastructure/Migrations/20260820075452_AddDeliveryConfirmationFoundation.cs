using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryConfirmationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryConfirmations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PodNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DispatchId = table.Column<int>(type: "integer", nullable: false),
                    DispatchPlanId = table.Column<int>(type: "integer", nullable: true),
                    DispatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReceiverContact = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DeliveryRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DamageRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_DeliveryConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryConfirmations_DispatchPlans_DispatchPlanId",
                        column: x => x.DispatchPlanId,
                        principalTable: "DispatchPlans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PodDocumentSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PodDocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PodAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeliveryConfirmationId = table.Column<int>(type: "integer", nullable: false),
                    AttachmentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SizeKb = table.Column<int>(type: "integer", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PodAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PodAttachments_DeliveryConfirmations_DeliveryConfirmationId",
                        column: x => x.DeliveryConfirmationId,
                        principalTable: "DeliveryConfirmations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PodTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeliveryConfirmationId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_PodTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PodTimelineEvents_DeliveryConfirmations_DeliveryConfirmatio~",
                        column: x => x.DeliveryConfirmationId,
                        principalTable: "DeliveryConfirmations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryConfirmations_CustomerId",
                table: "DeliveryConfirmations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryConfirmations_DispatchId",
                table: "DeliveryConfirmations",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryConfirmations_DispatchPlanId",
                table: "DeliveryConfirmations",
                column: "DispatchPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryConfirmations_PodNumber",
                table: "DeliveryConfirmations",
                column: "PodNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryConfirmations_Status",
                table: "DeliveryConfirmations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PodAttachments_DeliveryConfirmationId",
                table: "PodAttachments",
                column: "DeliveryConfirmationId");

            migrationBuilder.CreateIndex(
                name: "IX_PodDocumentSequences_Prefix",
                table: "PodDocumentSequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PodTimelineEvents_DeliveryConfirmationId",
                table: "PodTimelineEvents",
                column: "DeliveryConfirmationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PodAttachments");

            migrationBuilder.DropTable(
                name: "PodDocumentSequences");

            migrationBuilder.DropTable(
                name: "PodTimelineEvents");

            migrationBuilder.DropTable(
                name: "DeliveryConfirmations");
        }
    }
}
