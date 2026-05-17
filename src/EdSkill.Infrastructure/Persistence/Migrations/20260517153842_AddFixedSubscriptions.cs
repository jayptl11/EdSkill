using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EdSkill.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPlanId",
                table: "PaymentTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PriceVnd = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false, defaultValue: "VND"),
                    BillingCycle = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Monthly"),
                    ImmediateBonusPoints = table.Column<int>(type: "int", nullable: false),
                    WeeklyLearnerSessionBonusPoints = table.Column<int>(type: "int", nullable: false),
                    WeeklyCompanionSessionBonusPoints = table.Column<int>(type: "int", nullable: false),
                    LearnerTokenRewardRatePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CompanionTokenRewardRatePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CompanionDailySessionLimitOverride = table.Column<int>(type: "int", nullable: true),
                    CompanionBadgeText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasPriorityVisibility = table.Column<bool>(type: "bit", nullable: false),
                    BenefitsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.SubscriptionPlanId);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    UserSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.UserSubscriptionId);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "PaymentTransactionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "SubscriptionPlanId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "SubscriptionPlanId", "BenefitsJson", "Code", "CompanionBadgeText", "CompanionDailySessionLimitOverride", "CompanionTokenRewardRatePercent", "CreatedAt", "Currency", "DisplayOrder", "HasPriorityVisibility", "ImmediateBonusPoints", "IsActive", "LearnerTokenRewardRatePercent", "Name", "PriceVnd", "TargetRole", "UpdatedAt", "WeeklyCompanionSessionBonusPoints", "WeeklyLearnerSessionBonusPoints" },
                values: new object[,]
                {
                    { new Guid("92000000-0000-0000-0000-000000000001"), "[\"Tang ngay 200 Point\",\"Voucher 75% hang tuan\",\"Khong quang cao\",\"Uu tien matching\",\"Rebook nhanh\"]", "learner_pro", null, null, null, new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "VND", 1, false, 200, true, null, "Learner Pro", 119000, "Learner", new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), 0, 0 },
                    { new Guid("92000000-0000-0000-0000-000000000002"), "[\"Ed-Token bonus 30%\",\"Ho so noi bat hon\",\"Uu tien hien thi\",\"Mo nhieu slot hon\",\"Dashboard nang cao\"]", "companion_pro", "Companion Pro", 12, 30m, new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "VND", 2, true, 0, true, null, "Companion Pro", 79000, "Companion", new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), 0, 0 },
                    { new Guid("92000000-0000-0000-0000-000000000003"), "[\"200 Point cho buoi hoc dau tien trong tuan\",\"200 Point cho buoi huong dan dau tien trong tuan\",\"Learner token 10%\",\"Companion token 6%\",\"Bao gom quyen loi Learner va Companion\"]", "multi_role_pro", "Da nang Pro", 12, 6m, new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "VND", 3, true, 0, true, 10m, "Da nang Pro", 179000, "MultiRole", new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), 200, 200 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_SubscriptionPlanId",
                table: "PaymentTransactions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_IsActive_DisplayOrder",
                table: "SubscriptionPlans",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PaymentTransactionId",
                table: "UserSubscriptions",
                column: "PaymentTransactionId",
                unique: true,
                filter: "[PaymentTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PlanId",
                table: "UserSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_Status_ExpiresAt",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "Status", "ExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_SubscriptionPlans_SubscriptionPlanId",
                table: "PaymentTransactions",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "SubscriptionPlanId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_SubscriptionPlans_SubscriptionPlanId",
                table: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_SubscriptionPlanId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "PaymentTransactions");
        }
    }
}
