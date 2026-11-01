using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CmsDbContext))]
[Migration("20261101150000_AddCreatedDateTimeDefault")]
public partial class AddCreatedDateTimeDefault : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedDateTime",
            table: "Contents",
            type: "datetimeoffset",
            nullable: false,
            defaultValueSql: "SYSDATETIMEOFFSET()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedDateTime",
            table: "ContentsDisabled",
            type: "datetimeoffset",
            nullable: false,
            defaultValueSql: "SYSDATETIMEOFFSET()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedDateTime",
            table: "ContentVersions",
            type: "datetimeoffset",
            nullable: false,
            defaultValueSql: "SYSDATETIMEOFFSET()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedDateTime",
            table: "Events",
            type: "datetimeoffset",
            nullable: false,
            defaultValueSql: "SYSDATETIMEOFFSET()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedDateTime",
            table: "Contents",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldDefaultValueSql: "SYSDATETIMEOFFSET()");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedDateTime",
            table: "ContentsDisabled",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldDefaultValueSql: "SYSDATETIMEOFFSET()");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedDateTime",
            table: "ContentVersions",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldDefaultValueSql: "SYSDATETIMEOFFSET()");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedDateTime",
            table: "Events",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldDefaultValueSql: "SYSDATETIMEOFFSET()");
    }
}
