using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowDesk.Infrastructure.Persistence.Migrations;

public partial class NormalizeNamesAndPreventDuplicates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            SET NOCOUNT ON;

            CREATE TABLE #FlowDeskNames
            (
                TableName nvarchar(40) NOT NULL,
                Id int NOT NULL,
                ProjectId int NOT NULL,
                Value nvarchar(max)
                    COLLATE Latin1_General_100_CI_AS NOT NULL
            );

            INSERT INTO #FlowDeskNames
                (TableName, Id, ProjectId, Value)
            SELECT N'Projects', Id, 0, Name
            FROM Projects;

            INSERT INTO #FlowDeskNames
                (TableName, Id, ProjectId, Value)
            SELECT N'WorkItems', Id, ProjectId, Title
            FROM WorkItems;

            INSERT INTO #FlowDeskNames
                (TableName, Id, ProjectId, Value)
            SELECT N'TeamMembers', Id, 0, FullName
            FROM TeamMembers;

            UPDATE #FlowDeskNames
            SET Value = REPLACE(
                REPLACE(
                    Value COLLATE Latin1_General_100_BIN2,
                    N'ي',
                    N'ی'),
                N'ك',
                N'ک');

            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(9), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(10), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(11), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(12), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(13), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(133), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(160), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(5760), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8192), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8193), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8194), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8195), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8196), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8197), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8198), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8199), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8200), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8201), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8202), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8204), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8232), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8233), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8239), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(8287), N' ');
            UPDATE #FlowDeskNames SET Value = REPLACE(Value COLLATE Latin1_General_100_BIN2, NCHAR(12288), N' ');

            WHILE EXISTS
            (
                SELECT 1
                FROM #FlowDeskNames
                WHERE CHARINDEX(
                    N'  ',
                    Value COLLATE Latin1_General_100_BIN2) > 0
            )
            BEGIN
                UPDATE #FlowDeskNames
                SET Value = REPLACE(
                    Value COLLATE Latin1_General_100_BIN2,
                    N'  ',
                    N' ');
            END;

            UPDATE #FlowDeskNames
            SET Value = LTRIM(RTRIM(Value));

            DECLARE @problem nvarchar(2048);

            SELECT TOP (1) @problem = CONCAT(
                N'FlowDesk: invalid name in ', TableName,
                N', Id=', Id,
                N'. The normalized name must contain 1 to 200 characters.')
            FROM #FlowDeskNames
            WHERE DATALENGTH(Value) = 0
                OR DATALENGTH(Value) / 2 > 200
            ORDER BY TableName, Id;

            IF @problem IS NOT NULL
            BEGIN
                ;THROW 51001, @problem, 1;
            END;

            SET @problem = NULL;

            SELECT TOP (1) @problem = CONCAT(
                N'FlowDesk: duplicate names in ', a.TableName,
                N', Ids=', a.Id, N' and ', b.Id,
                N', ProjectId=', a.ProjectId,
                N'. Rename one record before retrying this migration.')
            FROM #FlowDeskNames a
            INNER JOIN #FlowDeskNames b
                ON b.TableName = a.TableName
                AND b.ProjectId = a.ProjectId
                AND b.Value = a.Value
                AND b.Id > a.Id
            ORDER BY a.TableName, a.ProjectId, a.Id, b.Id;

            IF @problem IS NOT NULL
            BEGIN
                ;THROW 51002, @problem, 1;
            END;

            UPDATE p
            SET Name = n.Value
            FROM Projects p
            INNER JOIN #FlowDeskNames n
                ON n.Id = p.Id
                AND n.TableName = N'Projects';

            UPDATE w
            SET Title = n.Value
            FROM WorkItems w
            INNER JOIN #FlowDeskNames n
                ON n.Id = w.Id
                AND n.TableName = N'WorkItems';

            UPDATE t
            SET FullName = n.Value
            FROM TeamMembers t
            INNER JOIN #FlowDeskNames n
                ON n.Id = t.Id
                AND n.TableName = N'TeamMembers';

            DROP TABLE #FlowDeskNames;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "Title",
            table: "WorkItems",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            collation: "Latin1_General_100_CI_AS",
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "FullName",
            table: "TeamMembers",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            collation: "Latin1_General_100_CI_AS",
            oldClrType: typeof(string),
            oldType: "nvarchar(200)",
            oldMaxLength: 200);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Projects",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            collation: "Latin1_General_100_CI_AS",
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "IX_WorkItems_ProjectId_Title",
            table: "WorkItems",
            columns: new[] { "ProjectId", "Title" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Projects_Name",
            table: "Projects",
            column: "Name",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_WorkItems_ProjectId_Title",
            table: "WorkItems");

        migrationBuilder.DropIndex(
            name: "IX_Projects_Name",
            table: "Projects");

        migrationBuilder.AlterColumn<string>(
            name: "Title",
            table: "WorkItems",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(200)",
            oldMaxLength: 200,
            oldCollation: "Latin1_General_100_CI_AS");

        migrationBuilder.AlterColumn<string>(
            name: "FullName",
            table: "TeamMembers",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(200)",
            oldMaxLength: 200,
            oldCollation: "Latin1_General_100_CI_AS");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Projects",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(200)",
            oldMaxLength: 200,
            oldCollation: "Latin1_General_100_CI_AS");
    }
}