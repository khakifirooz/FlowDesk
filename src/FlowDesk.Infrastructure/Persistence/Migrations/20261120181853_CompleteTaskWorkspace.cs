using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowDesk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteTaskWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAtUtc",
                table: "WorkItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAtUtc",
                table: "WorkItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "WorkItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WorkItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "WorkItems",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "WorkItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "Revision",
                table: "WorkItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    MembershipId = table.Column<int>(type: "int", nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectInvitations_ProjectMembers_ProjectId_MembershipId",
                        columns: x => new { x.ProjectId, x.MembershipId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "ProjectId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkItemActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkItemId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkItemActivities_WorkItems_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "WorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_ProjectId_ArchivedAtUtc",
                table: "WorkItems",
                columns: new[] { "ProjectId", "ArchivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_MembershipId",
                table: "ProjectInvitations",
                column: "MembershipId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_ProjectId_MembershipId",
                table: "ProjectInvitations",
                columns: new[] { "ProjectId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_TokenHash",
                table: "ProjectInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemActivities_WorkItemId_Id",
                table: "WorkItemActivities",
                columns: new[] { "WorkItemId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectInvitations");

            migrationBuilder.DropTable(
                name: "WorkItemActivities");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_ProjectId_ArchivedAtUtc",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "WorkItems");
        }
    }
}
