using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdSkill.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProfileMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE UserProfiles
                SET UpdatedAt = GETUTCDATE()
                WHERE UpdatedAt IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserProfiles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "UserProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Bio",
                table: "UserProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "UserProfiles",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "UserProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserProfiles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "UserProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Faculty",
                table: "UserProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActiveAt",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ReputationScore",
                table: "UserProfiles",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "SkillsToLearn",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'[]'");

            migrationBuilder.AddColumn<string>(
                name: "SkillsToTeach",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'[]'");

            migrationBuilder.AddColumn<int>(
                name: "TotalSessions",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "University",
                table: "UserProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearOfStudy",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE up
                SET
                    DisplayName = LEFT(
                        COALESCE(
                            NULLIF(LTRIM(RTRIM(
                                COALESCE(u.FirstName, N'') +
                                CASE
                                    WHEN COALESCE(u.LastName, N'') = N'' THEN N''
                                    ELSE N' ' + u.LastName
                                END
                            )), N''),
                            u.Username,
                            N'User'
                        ),
                        50
                    ),
                    LastActiveAt = COALESCE(u.LastLogin, up.LastActiveAt)
                FROM UserProfiles up
                INNER JOIN Users u ON u.UserId = up.UserId;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Faculty",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LastActiveAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ReputationScore",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SkillsToLearn",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SkillsToTeach",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TotalSessions",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "University",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "YearOfStudy",
                table: "UserProfiles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Bio",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
