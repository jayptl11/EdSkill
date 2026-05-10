using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EdSkill.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PointsSessionLedgerMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointWallets",
                columns: table => new
                {
                    PointWalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HeldBalance = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalEarned = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalSpent = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointWallets", x => x.PointWalletId);
                    table.ForeignKey(
                        name: "FK_PointWallets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Skill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    PointCost = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    JitsiRoomId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActualStartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDuration = table.Column<int>(type: "int", nullable: true),
                    LearnerConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CompanionConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CancelledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisbursedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Sessions_Users_CompanionId",
                        column: x => x.CompanionId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Users_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Key);
                    table.ForeignKey(
                        name: "FK_SystemConfigs_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemLedgerAccounts",
                columns: table => new
                {
                    SystemLedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Balance = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLedgerAccounts", x => x.SystemLedgerAccountId);
                });

            migrationBuilder.CreateTable(
                name: "PointTransactions",
                columns: table => new
                {
                    PointTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SystemLedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    BalanceBefore = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    HeldBalanceBefore = table.Column<int>(type: "int", nullable: false),
                    HeldBalanceAfter = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointTransactions", x => x.PointTransactionId);
                    table.ForeignKey(
                        name: "FK_PointTransactions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PointTransactions_SystemLedgerAccounts_SystemLedgerAccountId",
                        column: x => x.SystemLedgerAccountId,
                        principalTable: "SystemLedgerAccounts",
                        principalColumn: "SystemLedgerAccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PointTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "PolicyDocuments",
                keyColumn: "PolicyDocumentId",
                keyValue: new Guid("70000000-0000-0000-0000-000000000004"),
                column: "ContentMarkdown",
                value: "# Chính sách hủy phiên, hoàn điểm và no-show\n\n## Hủy phiên\n\n- Learner được hủy không mất Points khi phiên đang ở trạng thái hợp lệ và thời điểm hủy sớm hơn ít nhất 2 giờ trước giờ bắt đầu.\n- Nếu Learner hủy muộn sau mốc 2 giờ, hệ thống mặc định không hoàn Points cho Learner.\n\n## Phân bổ khi hủy muộn\n\n- Tỷ lệ mặc định của nền tảng là Companion nhận 80% và EdSkill ghi nhận 20% khi hủy muộn thuộc trường hợp áp dụng.\n- Cách xử lý cuối cùng vẫn phụ thuộc trạng thái phiên, log tham gia và quy trình dispute của hệ thống.\n\n## No-show và tranh chấp\n\n- No-show hoặc lỗi phát sinh trong phiên có thể được admin xem xét theo hồ sơ tham gia và log hệ thống.\n- Kết quả xử lý có thể là hoàn Points cho Learner, thanh toán cho Companion, chia một phần hoặc không hành động tùy case.");

            migrationBuilder.InsertData(
                table: "SystemConfigs",
                columns: new[] { "Key", "Description", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { "point.platform_fee_pct", "% phí nền tảng trên mỗi giao dịch completed.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "20" },
                    { "point.signup_bonus", "Điểm khởi đầu khi đăng ký.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "50" },
                    { "session.buffer_minutes", "Thời gian nghỉ tối thiểu giữa hai phiên.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "10" },
                    { "session.cancel_deadline_hours", "Số giờ trước phiên được hủy không mất điểm.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "2" },
                    { "session.late_cancel_companion_pct", "% điểm Companion nhận khi Learner hủy muộn.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "80" },
                    { "session.late_cancel_platform_pct", "% điểm nền tảng nhận khi Learner hủy muộn.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "20" },
                    { "session.max_per_day_per_companion", "Số phiên tối đa một Companion mở trong ngày.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "8" },
                    { "session.min_duration_minutes", "Thời lượng tối thiểu để phiên hợp lệ.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "10" },
                    { "token.companion_per_session", "Token Companion nhận sau mỗi phiên hợp lệ.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "3" },
                    { "token.daily_earn_limit", "Token tối đa nhận trong một ngày.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "20" },
                    { "token.learner_per_session", "Token Learner nhận sau mỗi phiên hợp lệ.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "5" },
                    { "token.weekly_earn_limit", "Token tối đa nhận trong một tuần.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, "100" }
                });

            migrationBuilder.InsertData(
                table: "SystemLedgerAccounts",
                columns: new[] { "SystemLedgerAccountId", "Code", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new Guid("90000000-0000-0000-0000-000000000001"), "platform_fee", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Platform Fee Ledger", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_SessionId_CreatedAt",
                table: "PointTransactions",
                columns: new[] { "SessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_SystemLedgerAccountId",
                table: "PointTransactions",
                column: "SystemLedgerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_UserId_CreatedAt",
                table: "PointTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PointWallets_UserId",
                table: "PointWallets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CompanionId_ScheduledAt",
                table: "Sessions",
                columns: new[] { "CompanionId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LearnerId",
                table: "Sessions",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status",
                table: "Sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_UpdatedBy",
                table: "SystemConfigs",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLedgerAccounts_Code",
                table: "SystemLedgerAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.Sql(
                """
                DECLARE @signupBonus INT = TRY_CAST((SELECT [Value] FROM SystemConfigs WHERE [Key] = N'point.signup_bonus') AS INT);
                IF @signupBonus IS NULL SET @signupBonus = 0;

                INSERT INTO PointWallets (PointWalletId, UserId, Balance, HeldBalance, TotalEarned, TotalSpent, CreatedAt, UpdatedAt)
                SELECT
                    NEWID(),
                    u.UserId,
                    CASE WHEN @signupBonus > 0 THEN @signupBonus ELSE 0 END,
                    0,
                    CASE WHEN @signupBonus > 0 THEN @signupBonus ELSE 0 END,
                    0,
                    GETUTCDATE(),
                    GETUTCDATE()
                FROM Users u
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM PointWallets w
                    WHERE w.UserId = u.UserId
                );

                IF @signupBonus > 0
                BEGIN
                    INSERT INTO PointTransactions (
                        PointTransactionId,
                        UserId,
                        SystemLedgerAccountId,
                        [Type],
                        Amount,
                        BalanceBefore,
                        BalanceAfter,
                        HeldBalanceBefore,
                        HeldBalanceAfter,
                        SessionId,
                        Note,
                        CreatedAt)
                    SELECT
                        NEWID(),
                        u.UserId,
                        NULL,
                        N'SignupBonus',
                        @signupBonus,
                        0,
                        @signupBonus,
                        0,
                        0,
                        NULL,
                        N'Migration backfill signup bonus',
                        GETUTCDATE()
                    FROM Users u
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM PointTransactions t
                        WHERE t.UserId = u.UserId
                          AND t.[Type] = N'SignupBonus'
                    );
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointTransactions");

            migrationBuilder.DropTable(
                name: "PointWallets");

            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "SystemLedgerAccounts");

            migrationBuilder.UpdateData(
                table: "PolicyDocuments",
                keyColumn: "PolicyDocumentId",
                keyValue: new Guid("70000000-0000-0000-0000-000000000004"),
                column: "ContentMarkdown",
                value: "# Chính sách hủy phiên, hoàn điểm và no-show\n\n## Hủy phiên\n\n- Learner được hủy không mất Points khi phiên đang ở trạng thái hợp lệ và thời điểm hủy sớm hơn ít nhất 2 giờ trước giờ bắt đầu.\n- Nếu Learner hủy muộn sau mốc 2 giờ, hệ thống mặc định không hoàn Points cho Learner.\n\n## Phân bổ khi hủy muộn\n\n- Tỷ lệ mặc định của nền tảng là Companion nhận 70% và EdSkill ghi nhận 30% khi hủy muộn thuộc trường hợp áp dụng.\n- Cách xử lý cuối cùng vẫn phụ thuộc trạng thái phiên, log tham gia và quy trình dispute của hệ thống.\n\n## No-show và tranh chấp\n\n- No-show hoặc lỗi phát sinh trong phiên có thể được admin xem xét theo hồ sơ tham gia và log hệ thống.\n- Kết quả xử lý có thể là hoàn Points cho Learner, thanh toán cho Companion, chia một phần hoặc không hành động tùy case.");
        }
    }
}
