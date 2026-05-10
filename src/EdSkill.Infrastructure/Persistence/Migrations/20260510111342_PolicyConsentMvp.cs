using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EdSkill.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PolicyConsentMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PolicyConsents",
                columns: table => new
                {
                    PolicyConsentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyConsents", x => x.PolicyConsentId);
                    table.ForeignKey(
                        name: "FK_PolicyConsents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PolicyDocuments",
                columns: table => new
                {
                    PolicyDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Audience = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PolicyType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresAcceptance = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyDocuments", x => x.PolicyDocumentId);
                });

            migrationBuilder.InsertData(
                table: "PolicyDocuments",
                columns: new[] { "PolicyDocumentId", "Audience", "Category", "ContentMarkdown", "CreatedAt", "EffectiveAt", "IsActive", "PolicyType", "RequiresAcceptance", "Slug", "Summary", "Title", "UpdatedAt", "Version" },
                values: new object[,]
                {
                    { new Guid("70000000-0000-0000-0000-000000000001"), "all", "legal", "# Điều khoản sử dụng EdSkill\n\nEdSkill là nền tảng kết nối Learner và Companion để trao đổi kỹ năng theo hình thức 1-1 trong phạm vi Phase 1 MVP.\n\n## Quy định chung\n\n- Người dùng phải cung cấp thông tin đúng sự thật khi đăng ký và cập nhật hồ sơ.\n- Learner không được tự đặt phiên với chính mình.\n- Mọi hoạt động trên nền tảng phải tuân thủ quy định pháp luật, quy tắc cộng đồng và các chính sách hiện hành của EdSkill.\n- EdSkill có quyền tạm khóa hoặc chấm dứt quyền truy cập khi phát hiện gian lận, lạm dụng hệ thống, quấy rối hoặc vi phạm chính sách.\n\n## Trách nhiệm của người dùng\n\n- Learner chịu trách nhiệm kiểm tra lịch học, số Points và thông tin phiên trước khi đặt lịch.\n- Companion chịu trách nhiệm mô tả kỹ năng, nội dung hỗ trợ và xác nhận hoặc từ chối phiên đúng luồng hệ thống.\n- Cả hai bên phải tham gia phiên với thái độ chuyên nghiệp, tôn trọng và không đăng tải nội dung trái phép.\n\n## Giới hạn trách nhiệm\n\n- EdSkill cung cấp nền tảng công nghệ và không cam kết kết quả học tập cụ thể.\n- Tranh chấp phát sinh từ hành vi người dùng có thể bị xử lý theo cơ chế hỗ trợ, hoàn điểm hoặc khóa tài khoản tùy mức độ.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), true, "Terms", true, "terms", "Quy định quyền, nghĩa vụ và giới hạn trách nhiệm giữa EdSkill, Learner và Companion.", "Điều khoản sử dụng nền tảng EdSkill", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), "2026-05-10.v1" },
                    { new Guid("70000000-0000-0000-0000-000000000002"), "all", "privacy", "# Chính sách riêng tư\n\nEdSkill thu thập dữ liệu cần thiết để vận hành đăng ký, hồ sơ người dùng, phiên học, lịch sử Points/Tokens và thông báo hệ thống.\n\n## Dữ liệu được xử lý\n\n- Thông tin tài khoản: email, username, họ tên, vai trò.\n- Thông tin hồ sơ: bio, kỹ năng, trường, khoa, ảnh đại diện và dữ liệu người dùng tự cập nhật.\n- Dữ liệu vận hành: lịch sử phiên, trạng thái tham gia, giao dịch Points/Tokens, nhật ký đồng ý chính sách.\n\n## Mục đích sử dụng\n\n- Xác thực tài khoản và bảo vệ an toàn hệ thống.\n- Vận hành tính năng đặt lịch, thông báo, hỗ trợ tranh chấp và báo cáo quản trị.\n- Cải thiện chất lượng dịch vụ trong phạm vi cho phép của chính sách này.\n\n## Cam kết\n\n- EdSkill không bán dữ liệu cá nhân của người dùng.\n- Chỉ nhân sự hoặc hệ thống được ủy quyền mới được truy cập dữ liệu phục vụ vận hành, hỗ trợ hoặc tuân thủ pháp lý.\n- Dữ liệu được lưu trong thời hạn phù hợp với nhu cầu vận hành, bảo mật và xử lý khiếu nại.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), true, "Privacy", true, "privacy", "Mô tả cách EdSkill thu thập, lưu trữ và xử lý dữ liệu tài khoản, phiên học và giao dịch.", "Chính sách riêng tư dữ liệu cá nhân", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), "2026-05-10.v1" },
                    { new Guid("70000000-0000-0000-0000-000000000003"), "all", "wallet", "# Chính sách Points và Tokens\n\n## Định nghĩa\n\n- Points là đơn vị thanh toán nội bộ của EdSkill để Learner đặt phiên với Companion.\n- Tokens là điểm thưởng loyalty để ghi nhận hoạt động tích cực, không dùng thanh toán trực tiếp cho phiên học.\n\n## Cam kết bắt buộc\n\n- Points và Tokens không phải tiền điện tử, tài sản ảo hoặc sản phẩm đầu tư.\n- Points và Tokens không được quy đổi thành tiền mặt, không được rút ra ngân hàng và không được giao dịch ngoài nền tảng.\n- Mọi thay đổi số dư Points hoặc Tokens đều phải đi qua giao dịch hệ thống và được ghi log.\n\n## Quy tắc sử dụng\n\n- Learner chỉ được đặt phiên khi có đủ Points theo chi phí phiên.\n- Companion nhận Points theo cơ chế giải ngân hợp lệ của hệ thống.\n- EdSkill có quyền điều chỉnh, hoàn hoặc khóa giao dịch khi phát hiện lỗi hệ thống, gian lận hoặc tranh chấp hợp lệ.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), true, "PointsTokens", true, "points-tokens", "Làm rõ Points là đơn vị nội bộ để thanh toán phiên, Tokens là loyalty reward; không phải crypto và không quy đổi tiền mặt.", "Chính sách Points và Tokens", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), "2026-05-10.v1" },
                    { new Guid("70000000-0000-0000-0000-000000000004"), "all", "sessions", "# Chính sách hủy phiên, hoàn điểm và no-show\n\n## Hủy phiên\n\n- Learner được hủy không mất Points khi phiên đang ở trạng thái hợp lệ và thời điểm hủy sớm hơn ít nhất 2 giờ trước giờ bắt đầu.\n- Nếu Learner hủy muộn sau mốc 2 giờ, hệ thống mặc định không hoàn Points cho Learner.\n\n## Phân bổ khi hủy muộn\n\n- Tỷ lệ mặc định của nền tảng là Companion nhận 70% và EdSkill ghi nhận 30% khi hủy muộn thuộc trường hợp áp dụng.\n- Cách xử lý cuối cùng vẫn phụ thuộc trạng thái phiên, log tham gia và quy trình dispute của hệ thống.\n\n## No-show và tranh chấp\n\n- No-show hoặc lỗi phát sinh trong phiên có thể được admin xem xét theo hồ sơ tham gia và log hệ thống.\n- Kết quả xử lý có thể là hoàn Points cho Learner, thanh toán cho Companion, chia một phần hoặc không hành động tùy case.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), true, null, false, "cancellation-refund", "Quy định hủy sớm, hủy muộn, hoàn Points, no-show và hướng xử lý dispute trong MVP.", "Chính sách hủy phiên, hoàn điểm và no-show", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), "2026-05-10.v1" },
                    { new Guid("70000000-0000-0000-0000-000000000005"), "learner", "community", "# Community Guidelines cho Learner\n\n- Tôn trọng thời gian, kỹ năng và công sức của Companion.\n- Mô tả nhu cầu học rõ ràng, không spam, không quấy rối và không yêu cầu nội dung trái pháp luật.\n- Tham gia phiên đúng giờ, xác nhận hoàn thành trung thực và gửi review có trách nhiệm.\n- Không chia sẻ tài khoản, không lạm dụng chính sách hoàn điểm và không dùng Points/Tokens sai mục đích.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), true, "CommunityGuidelines", false, "community-guidelines-learner", "Quy tắc ứng xử ngắn gọn dành cho Learner trong profile, chat và phiên học.", "Community Guidelines cho Learner", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), "2026-05-10.v1" },
                    { new Guid("70000000-0000-0000-0000-000000000006"), "companion", "community", "# Community Guidelines cho Companion\n\n- Cung cấp mô tả kỹ năng, phạm vi hỗ trợ và lịch rảnh trung thực.\n- Xác nhận hoặc từ chối phiên đúng thời gian, không dụ giao dịch ngoài nền tảng.\n- Tôn trọng Learner, không phân biệt đối xử, không lạm dụng quyền từ chối hoặc xác nhận hoàn thành sai sự thật.\n- Bảo mật dữ liệu phiên học và không sử dụng thông tin của Learner ngoài mục đích hỗ trợ trên EdSkill.", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), true, "CommunityGuidelines", false, "community-guidelines-companion", "Quy tắc ứng xử ngắn gọn dành cho Companion khi tạo hồ sơ, nhận phiên và hỗ trợ Learner.", "Community Guidelines cho Companion", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), "2026-05-10.v1" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyConsents_UserId_PolicyType_PolicyVersion",
                table: "PolicyConsents",
                columns: new[] { "UserId", "PolicyType", "PolicyVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDocuments_Slug_Version",
                table: "PolicyDocuments",
                columns: new[] { "Slug", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicyConsents");

            migrationBuilder.DropTable(
                name: "PolicyDocuments");
        }
    }
}
