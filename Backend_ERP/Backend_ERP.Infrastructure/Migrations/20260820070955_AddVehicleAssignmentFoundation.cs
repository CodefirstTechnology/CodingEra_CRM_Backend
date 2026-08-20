using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleAssignmentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransportDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransportNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DispatchId = table.Column<int>(type: "integer", nullable: false),
                    DispatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VehicleAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    AssignmentNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VehicleNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DriverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TransportCompanyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Route = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Destination = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedTimeHours = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualDeparture = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FuelNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LrId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportDocumentSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportDocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAssignmentDocumentSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAssignmentDocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DispatchId = table.Column<int>(type: "integer", nullable: false),
                    DispatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VehicleId = table.Column<int>(type: "integer", nullable: false),
                    VehicleName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    VehicleNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VehicleType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DriverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DriverContact = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TransportCompanyId = table.Column<int>(type: "integer", nullable: false),
                    TransportCompanyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LoadingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LoadingTime = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpectedDeparture = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AssignedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TransportId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_DispatchPlans_DispatchId",
                        column: x => x.DispatchId,
                        principalTable: "DispatchPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_TransportDetails_TransportId",
                        column: x => x.TransportId,
                        principalTable: "TransportDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAssignmentAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VehicleAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    AttachmentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SizeKb = table.Column<int>(type: "integer", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAssignmentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAssignmentAttachments_VehicleAssignments_VehicleAssi~",
                        column: x => x.VehicleAssignmentId,
                        principalTable: "VehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAssignmentTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VehicleAssignmentId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_VehicleAssignmentTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAssignmentTimelineEvents_VehicleAssignments_VehicleA~",
                        column: x => x.VehicleAssignmentId,
                        principalTable: "VehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransportDetails_DispatchId",
                table: "TransportDetails",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportDetails_Status",
                table: "TransportDetails",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TransportDetails_TransportNumber",
                table: "TransportDetails",
                column: "TransportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportDetails_VehicleAssignmentId",
                table: "TransportDetails",
                column: "VehicleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportDocumentSequences_Prefix",
                table: "TransportDocumentSequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignmentAttachments_VehicleAssignmentId",
                table: "VehicleAssignmentAttachments",
                column: "VehicleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignmentDocumentSequences_Prefix",
                table: "VehicleAssignmentDocumentSequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_AssignmentNumber",
                table: "VehicleAssignments",
                column: "AssignmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_DispatchId",
                table: "VehicleAssignments",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_LoadingDate",
                table: "VehicleAssignments",
                column: "LoadingDate");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_Status",
                table: "VehicleAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_TransportCompanyId",
                table: "VehicleAssignments",
                column: "TransportCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_TransportId",
                table: "VehicleAssignments",
                column: "TransportId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_VehicleNumber",
                table: "VehicleAssignments",
                column: "VehicleNumber");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignmentTimelineEvents_VehicleAssignmentId",
                table: "VehicleAssignmentTimelineEvents",
                column: "VehicleAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransportDocumentSequences");

            migrationBuilder.DropTable(
                name: "VehicleAssignmentAttachments");

            migrationBuilder.DropTable(
                name: "VehicleAssignmentDocumentSequences");

            migrationBuilder.DropTable(
                name: "VehicleAssignmentTimelineEvents");

            migrationBuilder.DropTable(
                name: "VehicleAssignments");

            migrationBuilder.DropTable(
                name: "TransportDetails");
        }
    }
}
