using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchStatusTrackingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DispatchStatusTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DispatchId = table.Column<int>(type: "integer", nullable: false),
                    DispatchPlanId = table.Column<int>(type: "integer", nullable: true),
                    DispatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrentStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WarehouseStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VehicleStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TransportStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DispatchDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDelivery = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualDelivery = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DelayHours = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchStatusTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchStatusTracks_DispatchPlans_DispatchPlanId",
                        column: x => x.DispatchPlanId,
                        principalTable: "DispatchPlans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DispatchStatusTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DispatchStatusTrackId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_DispatchStatusTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchStatusTimelineEvents_DispatchStatusTracks_DispatchS~",
                        column: x => x.DispatchStatusTrackId,
                        principalTable: "DispatchStatusTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchStatusTimelineEvents_DispatchStatusTrackId",
                table: "DispatchStatusTimelineEvents",
                column: "DispatchStatusTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchStatusTracks_CurrentStatus",
                table: "DispatchStatusTracks",
                column: "CurrentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchStatusTracks_DispatchId",
                table: "DispatchStatusTracks",
                column: "DispatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchStatusTracks_DispatchPlanId",
                table: "DispatchStatusTracks",
                column: "DispatchPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispatchStatusTimelineEvents");

            migrationBuilder.DropTable(
                name: "DispatchStatusTracks");
        }
    }
}
