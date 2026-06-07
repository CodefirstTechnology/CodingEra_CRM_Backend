using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddItemMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_attributes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    is_variant_attribute = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_attributes", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_attributes_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_item_attributes_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "item_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_groups_item_groups_parent_id",
                        column: x => x.parent_id,
                        principalTable: "item_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_groups_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_item_groups_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "item_attribute_values",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    attribute_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_attribute_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_attribute_values_item_attributes_attribute_id",
                        column: x => x.attribute_id,
                        principalTable: "item_attributes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    item_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    item_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    item_group_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    has_variants = table.Column<bool>(type: "boolean", nullable: false),
                    parent_item_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_items_item_groups_item_group_id",
                        column: x => x.item_group_id,
                        principalTable: "item_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_items_items_parent_item_id",
                        column: x => x.parent_item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_items_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_items_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "item_specifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    spec_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    spec_value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_specifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_specifications_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_template_attributes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    attribute_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_template_attributes", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_template_attributes_item_attributes_attribute_id",
                        column: x => x.attribute_id,
                        principalTable: "item_attributes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_template_attributes_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_variant_attribute_values",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    attribute_id = table.Column<int>(type: "integer", nullable: false),
                    attribute_value_id = table.Column<int>(type: "integer", nullable: true),
                    custom_value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_variant_attribute_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_variant_attribute_values_item_attribute_values_attribu~",
                        column: x => x.attribute_value_id,
                        principalTable: "item_attribute_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_item_variant_attribute_values_item_attributes_attribute_id",
                        column: x => x.attribute_id,
                        principalTable: "item_attributes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_variant_attribute_values_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_attribute_values_attribute_id",
                table: "item_attribute_values",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_attributes_code",
                table: "item_attributes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_attributes_created_by",
                table: "item_attributes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_item_attributes_updated_by",
                table: "item_attributes",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_item_groups_created_by",
                table: "item_groups",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_item_groups_name",
                table: "item_groups",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_item_groups_parent_id",
                table: "item_groups",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_groups_updated_by",
                table: "item_groups",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_item_specifications_item_id",
                table: "item_specifications",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_template_attributes_attribute_id",
                table: "item_template_attributes",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_template_attributes_item_id_attribute_id",
                table: "item_template_attributes",
                columns: new[] { "item_id", "attribute_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_variant_attribute_values_attribute_id",
                table: "item_variant_attribute_values",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_variant_attribute_values_attribute_value_id",
                table: "item_variant_attribute_values",
                column: "attribute_value_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_variant_attribute_values_item_id",
                table: "item_variant_attribute_values",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_created_by",
                table: "items",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_items_item_code",
                table: "items",
                column: "item_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_item_group_id",
                table: "items",
                column: "item_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_parent_item_id",
                table: "items",
                column: "parent_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_updated_by",
                table: "items",
                column: "updated_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_specifications");

            migrationBuilder.DropTable(
                name: "item_template_attributes");

            migrationBuilder.DropTable(
                name: "item_variant_attribute_values");

            migrationBuilder.DropTable(
                name: "item_attribute_values");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "item_attributes");

            migrationBuilder.DropTable(
                name: "item_groups");
        }
    }
}
