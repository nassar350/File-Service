using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "varchar", nullable: false),
                    entity_type = table.Column<string>(type: "varchar", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "varchar", nullable: false),
                    provider_key = table.Column<string>(type: "varchar", nullable: false),
                    storage_region = table.Column<string>(type: "varchar", nullable: false),
                    original_filename = table.Column<string>(type: "varchar", nullable: false),
                    mime_type = table.Column<string>(type: "varchar", nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    checksum = table.Column<string>(type: "varchar", nullable: false),
                    metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "varchar", nullable: false),
                    staging_expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    committed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "file_cleanup_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "varchar", nullable: false),
                    status = table.Column<string>(type: "varchar", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_cleanup_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_file_cleanup_logs_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "file_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "varchar", nullable: false),
                    metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_file_events_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_cleanup_logs_created_at",
                table: "file_cleanup_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_file_cleanup_logs_file_id",
                table: "file_cleanup_logs",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_events_created_at",
                table: "file_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_file_events_file_id",
                table: "file_events",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "idx_files_staging_expiry",
                table: "files",
                column: "staging_expires_at",
                filter: "status = 'Staged'");

            migrationBuilder.CreateIndex(
                name: "ix_files_entity_id",
                table: "files",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_files_owner_id",
                table: "files",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_files_status",
                table: "files",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_files_tenant_id",
                table: "files",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_cleanup_logs");

            migrationBuilder.DropTable(
                name: "file_events");

            migrationBuilder.DropTable(
                name: "files");
        }
    }
}
