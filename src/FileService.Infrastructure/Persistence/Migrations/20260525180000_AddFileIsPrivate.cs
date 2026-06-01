using FileService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FileServiceDbContext))]
[Migration("20260525180000_AddFileIsPrivate")]
public partial class AddFileIsPrivate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_private",
            table: "files",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "is_private",
            table: "files");
    }
}
