using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdSkill.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Faculty",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "University",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "YearOfStudy",
                table: "UserProfiles");

            migrationBuilder.AddColumn<string>(
                name: "DegreeUrl",
                table: "UserProfiles",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DegreeUrl",
                table: "UserProfiles");

            migrationBuilder.AddColumn<string>(
                name: "Faculty",
                table: "UserProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

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
        }
    }
}
