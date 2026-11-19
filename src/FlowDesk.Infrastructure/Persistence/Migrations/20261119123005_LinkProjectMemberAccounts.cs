using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowDesk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkProjectMemberAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountUserId",
                table: "ProjectMembers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_AccountUserId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "AccountUserId" },
                unique: true,
                filter: "[AccountUserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_ProjectId_AccountUserId",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "AccountUserId",
                table: "ProjectMembers");
        }
    }
}
