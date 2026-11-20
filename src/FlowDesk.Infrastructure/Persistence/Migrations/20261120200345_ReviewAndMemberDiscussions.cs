using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowDesk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReviewAndMemberDiscussions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CompletionRecipientCaptured",
                table: "WorkItemActivities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CompletionRecipientMembershipId",
                table: "WorkItemActivities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionRecipientUserId",
                table: "WorkItemActivities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_WorkItemActivities_WorkItemId_Id",
                table: "WorkItemActivities",
                columns: new[] { "WorkItemId", "Id" });

            migrationBuilder.CreateTable(
                name: "ProjectMemberComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    SubjectMemberId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMemberComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMemberComments_ProjectMembers_ProjectId_SubjectMemberId",
                        columns: x => new { x.ProjectId, x.SubjectMemberId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "ProjectId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkItemReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkItemId = table.Column<int>(type: "int", nullable: false),
                    CompletionActivityId = table.Column<int>(type: "int", nullable: false),
                    RecipientMembershipId = table.Column<int>(type: "int", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Score = table.Column<int>(type: "int", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReviewerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemReviews", x => x.Id);
                    table.CheckConstraint("CK_WorkItemReviews_Content", "[Score] IS NOT NULL OR [Feedback] IS NOT NULL");
                    table.CheckConstraint("CK_WorkItemReviews_Score", "[Score] IS NULL OR [Score] BETWEEN 0 AND 10");
                    table.ForeignKey(
                        name: "FK_WorkItemReviews_WorkItemActivities_WorkItemId_CompletionActivityId",
                        columns: x => new { x.WorkItemId, x.CompletionActivityId },
                        principalTable: "WorkItemActivities",
                        principalColumns: new[] { "WorkItemId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberComments_ProjectId_SubjectMemberId_Id",
                table: "ProjectMemberComments",
                columns: new[] { "ProjectId", "SubjectMemberId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemReviews_CompletionActivityId",
                table: "WorkItemReviews",
                column: "CompletionActivityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemReviews_WorkItemId_CompletionActivityId",
                table: "WorkItemReviews",
                columns: new[] { "WorkItemId", "CompletionActivityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectMemberComments");

            migrationBuilder.DropTable(
                name: "WorkItemReviews");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_WorkItemActivities_WorkItemId_Id",
                table: "WorkItemActivities");

            migrationBuilder.DropColumn(
                name: "CompletionRecipientCaptured",
                table: "WorkItemActivities");

            migrationBuilder.DropColumn(
                name: "CompletionRecipientMembershipId",
                table: "WorkItemActivities");

            migrationBuilder.DropColumn(
                name: "CompletionRecipientUserId",
                table: "WorkItemActivities");
        }
    }
}
