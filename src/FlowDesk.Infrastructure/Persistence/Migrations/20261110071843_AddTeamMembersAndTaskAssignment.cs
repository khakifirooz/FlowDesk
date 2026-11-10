using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowDesk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMembersAndTaskAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_ProjectId",
                table: "WorkItems");

            migrationBuilder.AddColumn<int>(
                name: "AssignedProjectMemberId",
                table: "WorkItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    TeamMemberId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => x.Id);
                    table.UniqueConstraint("AK_ProjectMembers_ProjectId_Id", x => new { x.ProjectId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_TeamMembers_TeamMemberId",
                        column: x => x.TeamMemberId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_ProjectId_AssignedProjectMemberId",
                table: "WorkItems",
                columns: new[] { "ProjectId", "AssignedProjectMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_TeamMemberId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "TeamMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_TeamMemberId",
                table: "ProjectMembers",
                column: "TeamMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkItems_ProjectMembers_ProjectId_AssignedProjectMemberId",
                table: "WorkItems",
                columns: new[] { "ProjectId", "AssignedProjectMemberId" },
                principalTable: "ProjectMembers",
                principalColumns: new[] { "ProjectId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkItems_ProjectMembers_ProjectId_AssignedProjectMemberId",
                table: "WorkItems");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_ProjectId_AssignedProjectMemberId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "AssignedProjectMemberId",
                table: "WorkItems");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_ProjectId",
                table: "WorkItems",
                column: "ProjectId");
        }
    }
}
