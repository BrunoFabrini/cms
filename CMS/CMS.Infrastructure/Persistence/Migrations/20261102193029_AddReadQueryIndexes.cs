using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReadQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentVersions_ContentId",
                table: "ContentVersions");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status_CreatedDateTime",
                table: "Events",
                columns: new[] { "Status", "CreatedDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status_LastProcessedAtDateTime",
                table: "Events",
                columns: new[] { "Status", "LastProcessedAtDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentVersions_ContentId_VersionNumber",
                table: "ContentVersions",
                columns: new[] { "ContentId", "VersionNumber" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "Published", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Contents_PublicId",
                table: "Contents",
                column: "PublicId")
                .Annotation("SqlServer:Include", new[] { "Enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Status_CreatedDateTime",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_Status_LastProcessedAtDateTime",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_ContentVersions_ContentId_VersionNumber",
                table: "ContentVersions");

            migrationBuilder.DropIndex(
                name: "IX_Contents_PublicId",
                table: "Contents");

            migrationBuilder.CreateIndex(
                name: "IX_ContentVersions_ContentId",
                table: "ContentVersions",
                column: "ContentId");
        }
    }
}
