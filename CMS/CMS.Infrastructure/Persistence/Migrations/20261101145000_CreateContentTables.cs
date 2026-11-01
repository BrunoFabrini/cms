using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CmsDbContext))]
[Migration("20261101145000_CreateContentTables")]
public partial class CreateContentTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[Contents]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Contents]
                (
                    [ContentId] int IDENTITY(1,1) NOT NULL,
                    [CreatedDateTime] datetimeoffset NOT NULL,
                    CONSTRAINT [PK_Content_ContentId] PRIMARY KEY ([ContentId])
                );
            END;
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[ContentsDisabled]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ContentsDisabled]
                (
                    [ContentDisabledId] int IDENTITY(1,1) NOT NULL,
                    [CreatedDateTime] datetimeoffset NOT NULL,
                    CONSTRAINT [PK_ContentDisabled_ContentDisabledId] PRIMARY KEY ([ContentDisabledId])
                );
            END;
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[ContentVersions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ContentVersions]
                (
                    [ContentVersionId] int IDENTITY(1,1) NOT NULL,
                    [CreatedDateTime] datetimeoffset NOT NULL,
                    CONSTRAINT [PK_ContentVersion_ContentVersionId] PRIMARY KEY ([ContentVersionId])
                );
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Tables may already exist outside the migration history, so rollback must not drop them.
    }
}
