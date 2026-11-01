using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CmsDbContext))]
[Migration("20261101172000_AddContentEntityFields")]
public partial class AddContentEntityFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ContentsDisabled");

        migrationBuilder.AddColumn<string>(
            name: "PublicId",
            table: "Contents",
            type: "varchar(100)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<bool>(
            name: "Enabled",
            table: "Contents",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "Published",
            table: "Contents",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "ContentId",
            table: "ContentVersions",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "Content",
            table: "ContentVersions",
            type: "varchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<bool>(
            name: "IsPublished",
            table: "ContentVersions",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "Timestamp",
            table: "ContentVersions",
            type: "datetimeoffset",
            nullable: false,
            defaultValueSql: "SYSDATETIMEOFFSET()");

        migrationBuilder.AddColumn<int>(
            name: "VersionNumber",
            table: "ContentVersions",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_ContentVersions_ContentId",
            table: "ContentVersions",
            column: "ContentId");

        migrationBuilder.AddForeignKey(
            name: "FK_ContentVersions_Contents_ContentId",
            table: "ContentVersions",
            column: "ContentId",
            principalTable: "Contents",
            principalColumn: "ContentId",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ContentVersions_Contents_ContentId",
            table: "ContentVersions");

        migrationBuilder.DropIndex(
            name: "IX_ContentVersions_ContentId",
            table: "ContentVersions");

        migrationBuilder.DropColumn(
            name: "PublicId",
            table: "Contents");

        migrationBuilder.DropColumn(
            name: "Enabled",
            table: "Contents");

        migrationBuilder.DropColumn(
            name: "Published",
            table: "Contents");

        migrationBuilder.DropColumn(
            name: "ContentId",
            table: "ContentVersions");

        migrationBuilder.DropColumn(
            name: "Content",
            table: "ContentVersions");

        migrationBuilder.DropColumn(
            name: "IsPublished",
            table: "ContentVersions");

        migrationBuilder.DropColumn(
            name: "Timestamp",
            table: "ContentVersions");

        migrationBuilder.DropColumn(
            name: "VersionNumber",
            table: "ContentVersions");

        migrationBuilder.CreateTable(
            name: "ContentsDisabled",
            columns: table => new
            {
                ContentDisabledId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CreatedDateTime = table.Column<DateTimeOffset>(
                    type: "datetimeoffset",
                    nullable: false,
                    defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContentDisabled_ContentDisabledId", x => x.ContentDisabledId);
            });
    }
}
