using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CmsDbContext))]
[Migration("20261101140151_AddEventEntity")]
public partial class AddEventEntity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                ContentId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Payload = table.Column<string>(type: "varchar(max)", nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false),
                ErrorMessage = table.Column<string>(type: "varchar(250)", nullable: true),
                LastProcessedAtDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset(2)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Event_EventId", x => x.ContentId);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Events");
    }
}
