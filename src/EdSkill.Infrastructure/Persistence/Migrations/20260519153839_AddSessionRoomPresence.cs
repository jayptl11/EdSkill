using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EdSkill.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRoomPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionPresenceSegments",
                columns: table => new
                {
                    SessionPresenceSegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPresenceSegments", x => x.SessionPresenceSegmentId);
                    table.ForeignKey(
                        name: "FK_SessionPresenceSegments_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionPresenceSegments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "SystemConfigs",
                columns: new[] { "Key", "Description", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { "session.join_early_minutes", "So phut duoc vao phong truoc gio hoc.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "10" },
                    { "session.join_late_grace_minutes", "So phut cho phep vao phong sau gio ket thuc du kien.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "30" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionPresenceSegments_SessionId_UserId",
                table: "SessionPresenceSegments",
                columns: new[] { "SessionId", "UserId" },
                unique: true,
                filter: "[LeftAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPresenceSegments_SessionId_UserId_JoinedAt",
                table: "SessionPresenceSegments",
                columns: new[] { "SessionId", "UserId", "JoinedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionPresenceSegments_UserId",
                table: "SessionPresenceSegments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionPresenceSegments");

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.join_early_minutes");

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.join_late_grace_minutes");
        }
    }
}
