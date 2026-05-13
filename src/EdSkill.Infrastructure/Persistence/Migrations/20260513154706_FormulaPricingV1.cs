using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdSkill.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FormulaPricingV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CredentialUrls",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'[]'");

            migrationBuilder.AddColumn<int>(
                name: "BasePointCost",
                table: "Skills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanionPayoutPoints",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CredentialBonusPointsSnapshot",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMultiplierPercentSnapshot",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DurationOptions",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'[]'");

            migrationBuilder.AddColumn<int>(
                name: "LearnerChargePoints",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlatformFeePoints",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricingModel",
                table: "Sessions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "LegacyManual");

            migrationBuilder.AddColumn<int>(
                name: "SelectedDurationMinutes",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SkillBasePointsSnapshot",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SkillId",
                table: "Sessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TokenTransactions",
                columns: table => new
                {
                    TokenTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenTransactions", x => x.TokenTransactionId);
                    table.ForeignKey(
                        name: "FK_TokenTransactions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "point.platform_fee_pct",
                column: "Description",
                value: "% phi nen tang tren moi giao dich completed legacy.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "point.signup_bonus",
                column: "Description",
                value: "Diem khoi dau khi dang ky.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.buffer_minutes",
                column: "Description",
                value: "Thoi gian nghi toi thieu giua hai phien.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.cancel_deadline_hours",
                column: "Description",
                value: "So gio truoc phien duoc huy khong mat diem.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.late_cancel_companion_pct",
                column: "Description",
                value: "% diem Companion nhan khi Learner huy muon.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.late_cancel_platform_pct",
                column: "Description",
                value: "% diem nen tang nhan khi Learner huy muon.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.max_per_day_per_companion",
                column: "Description",
                value: "So phien toi da mot Companion mo trong ngay.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.min_duration_minutes",
                column: "Description",
                value: "Thoi luong toi thieu de phien hop le.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "token.companion_per_session",
                column: "Description",
                value: "Token Companion nhan sau moi phien hop le legacy.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "token.daily_earn_limit",
                column: "Description",
                value: "Token toi da nhan trong mot ngay.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "token.learner_per_session",
                column: "Description",
                value: "Token Learner nhan sau moi phien hop le legacy.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "token.weekly_earn_limit",
                column: "Description",
                value: "Token toi da nhan trong mot tuan.");

            migrationBuilder.InsertData(
                table: "SystemConfigs",
                columns: new[] { "Key", "Description", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[] { "point.platform_markup_pct", "% markup cong len gia Companion cho Formula Pricing.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "25" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SkillId",
                table: "Sessions",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_SessionId_CreatedAt",
                table: "TokenTransactions",
                columns: new[] { "SessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_UserId_CreatedAt",
                table: "TokenTransactions",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_SkillId",
                table: "Sessions");

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "point.platform_markup_pct");

            migrationBuilder.DropColumn(
                name: "CredentialUrls",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BasePointCost",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "CompanionPayoutPoints",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "CredentialBonusPointsSnapshot",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "DurationMultiplierPercentSnapshot",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "DurationOptions",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "LearnerChargePoints",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PlatformFeePoints",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PricingModel",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SelectedDurationMinutes",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SkillBasePointsSnapshot",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SkillId",
                table: "Sessions");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "point.platform_fee_pct",
                column: "Description",
                value: "% phí nền tảng trên mỗi giao dịch completed.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "point.signup_bonus",
                column: "Description",
                value: "Điểm khởi đầu khi đăng ký.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.buffer_minutes",
                column: "Description",
                value: "Thời gian nghỉ tối thiểu giữa hai phiên.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.cancel_deadline_hours",
                column: "Description",
                value: "Số giờ trước phiên được hủy không mất điểm.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.late_cancel_companion_pct",
                column: "Description",
                value: "% điểm Companion nhận khi Learner hủy muộn.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.late_cancel_platform_pct",
                column: "Description",
                value: "% điểm nền tảng nhận khi Learner hủy muộn.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.max_per_day_per_companion",
                column: "Description",
                value: "Số phiên tối đa một Companion mở trong ngày.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "session.min_duration_minutes",
                column: "Description",
                value: "Thời lượng tối thiểu để phiên hợp lệ.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "token.companion_per_session",
                column: "Description",
                value: "Token Companion nhận sau mỗi phiên hợp lệ.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "token.daily_earn_limit",
                column: "Description",
                value: "Token tối đa nhận trong một ngày.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "token.learner_per_session",
                column: "Description",
                value: "Token Learner nhận sau mỗi phiên hợp lệ.");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Key",
                keyValue: "token.weekly_earn_limit",
                column: "Description",
                value: "Token tối đa nhận trong một tuần.");
        }
    }
}
