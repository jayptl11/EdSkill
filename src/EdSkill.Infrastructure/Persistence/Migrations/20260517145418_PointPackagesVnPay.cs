using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EdSkill.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PointPackagesVnPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointPackages",
                columns: table => new
                {
                    PointPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    BonusPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PriceVnd = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false, defaultValue: "VND"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BadgeText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointPackages", x => x.PointPackageId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    PaymentTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AmountVnd = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false, defaultValue: "VND"),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PaymentUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.PaymentTransactionId);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_PointPackages_PointPackageId",
                        column: x => x.PointPackageId,
                        principalTable: "PointPackages",
                        principalColumn: "PointPackageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PointPackages",
                columns: new[] { "PointPackageId", "BadgeText", "Code", "CreatedAt", "Currency", "Description", "DisplayOrder", "EndsAt", "IsActive", "Name", "Points", "PriceVnd", "StartsAt", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("91000000-0000-0000-0000-000000000001"), null, "goi_1", new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "VND", "Gói nạp 500 Points.", 1, null, true, "Gói 1", 500, 59000, null, new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("91000000-0000-0000-0000-000000000002"), null, "goi_2", new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "VND", "Gói nạp 1.000 Points.", 2, null, true, "Gói 2", 1000, 99000, null, new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("91000000-0000-0000-0000-000000000003"), null, "goi_3", new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "VND", "Gói nạp 2.000 Points.", 3, null, true, "Gói 3", 2000, 169000, null, new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("91000000-0000-0000-0000-000000000004"), null, "goi_4", new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "VND", "Gói nạp 5.000 Points.", 4, null, true, "Gói 4", 5000, 379000, null, new DateTime(2026, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PointPackageId",
                table: "PaymentTransactions",
                column: "PointPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Provider_ProviderTransactionId",
                table: "PaymentTransactions",
                columns: new[] { "Provider", "ProviderTransactionId" },
                unique: true,
                filter: "[ProviderTransactionId] IS NOT NULL AND [Status] = 'Success'");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserId_Status_CreatedAt",
                table: "PaymentTransactions",
                columns: new[] { "UserId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PointPackages_Code",
                table: "PointPackages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointPackages_IsDeleted_IsActive_DisplayOrder",
                table: "PointPackages",
                columns: new[] { "IsDeleted", "IsActive", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "PointPackages");
        }
    }
}
