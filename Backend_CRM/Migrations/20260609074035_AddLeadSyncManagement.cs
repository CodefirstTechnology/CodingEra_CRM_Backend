using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadSyncManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lead_sync_interval_options",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    hours = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_sync_interval_options", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lead_sync_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    marker_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    api_integration_ready = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_sync_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lead_sync_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    sync_type = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_received = table.Column<int>(type: "integer", nullable: false),
                    total_created = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    triggered_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_sync_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_lead_sync_logs_lead_sync_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "lead_sync_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lead_sync_logs_users_triggered_by_user_id",
                        column: x => x.triggered_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "lead_sync_round_robin_states",
                columns: table => new
                {
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    next_index = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_sync_round_robin_states", x => x.source_id);
                    table.ForeignKey(
                        name: "FK_lead_sync_round_robin_states_lead_sync_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "lead_sync_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lead_sync_source_assignments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_sync_source_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_lead_sync_source_assignments_lead_sync_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "lead_sync_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lead_sync_source_assignments_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_lead_sync_source_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lead_sync_source_configs",
                columns: table => new
                {
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    auto_sync_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    interval_option_id = table.Column<int>(type: "integer", nullable: true),
                    last_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_sync_source_configs", x => x.source_id);
                    table.ForeignKey(
                        name: "FK_lead_sync_source_configs_lead_sync_interval_options_interva~",
                        column: x => x.interval_option_id,
                        principalTable: "lead_sync_interval_options",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_lead_sync_source_configs_lead_sync_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "lead_sync_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lead_sync_source_configs_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_interval_options_hours",
                table: "lead_sync_interval_options",
                column: "hours",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_logs_source_id_started_at",
                table: "lead_sync_logs",
                columns: new[] { "source_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_logs_triggered_by_user_id",
                table: "lead_sync_logs",
                column: "triggered_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_source_assignments_created_by",
                table: "lead_sync_source_assignments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_source_assignments_source_id_sort_order",
                table: "lead_sync_source_assignments",
                columns: new[] { "source_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_source_assignments_source_id_user_id",
                table: "lead_sync_source_assignments",
                columns: new[] { "source_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_source_assignments_user_id",
                table: "lead_sync_source_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_source_configs_interval_option_id",
                table: "lead_sync_source_configs",
                column: "interval_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_source_configs_next_sync_at",
                table: "lead_sync_source_configs",
                column: "next_sync_at");

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_source_configs_updated_by",
                table: "lead_sync_source_configs",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_sources_code",
                table: "lead_sync_sources",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_sources_marker_name",
                table: "lead_sync_sources",
                column: "marker_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lead_sync_logs");

            migrationBuilder.DropTable(
                name: "lead_sync_round_robin_states");

            migrationBuilder.DropTable(
                name: "lead_sync_source_assignments");

            migrationBuilder.DropTable(
                name: "lead_sync_source_configs");

            migrationBuilder.DropTable(
                name: "lead_sync_interval_options");

            migrationBuilder.DropTable(
                name: "lead_sync_sources");
        }
    }
}
